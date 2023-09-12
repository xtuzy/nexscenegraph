﻿//
// Copyright 2018-2021 Sean Spicer 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid.SceneGraph.InputAdapter;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;
using static Veldrid.Sdl2.Sdl2Native;

namespace Veldrid.SceneGraph.Viewer
{
    internal class EndFrameEvent : IEndFrameEvent
    {
        internal EndFrameEvent(float frameTime)
        {
            FrameTime = frameTime;
        }

        public float FrameTime { get; }
    }

    internal class ResizedEvent : IResizedEvent
    {
        internal ResizedEvent(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; }
        public int Height { get; }
    }

    internal class FrameCaptureEventHandler : UiEventHandler
    {
        public override bool Handle(IUiEventAdapter eventAdapter, IUiActionAdapter actionAdapter)
        {
            if (actionAdapter is IView view)
                if (eventAdapter.Key == IUiEventAdapter.KeySymbol.KeyC &&
                    (eventAdapter.EventType & IUiEventAdapter.EventTypeValue.KeyDown) != 0 &&
                    (eventAdapter.ModKeyMask & IUiEventAdapter.ModKeyMaskType.ModKeyShift) != 0 &&
                    (eventAdapter.ModKeyMask & IUiEventAdapter.ModKeyMaskType.ModKeyCtl) != 0)
                {
                    view?.Camera?.Renderer.CaptureNextFrame();
                    return true;
                }

            return false;
        }
    }

    public class SimpleViewer : View, IViewer
    {
        private const uint NFramesInBuffer = 30;
        private readonly double[] _frameTimeBuff = new double[NFramesInBuffer];
        private readonly Sdl2Window _window;
        private DisposeCollectorResourceFactory _factory;
        private bool _firstFrame = true;
        private double _fpsDrawTimeAccumulator;
        private ulong _frameCounter;
        private double _frameTimeAccumulator;
        private ulong _globalFrameCounter;

        private readonly GraphicsBackend _preferredBackend;
        private double _previousElapsed;

        private readonly SceneContext _sceneContext;
        private Stopwatch _stopwatch;

        private readonly ISubject<IEvent> _viewerInputEvents;
        private bool _windowResized = true;

        //public IObservable<IInputStateSnapshot> ViewerInputEvents => _viewerInputEvents;


        //public IObservable<

        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        //
        // PRIVATE Properties
        //
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        private string _windowTitle = string.Empty;

        private readonly SDL_EventFilter ResizeEventFilter;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="title"></param>
        //
        // TODO: remove unsafe once Veldrid.SDL2 implements resize fix.
        //
        protected unsafe SimpleViewer(string title, TextureSampleCount fsaaCount, GraphicsBackend? preferredBackend)
            : base(960, 540, 1000.0f)
        {
            _preferredBackend = DisplaySettings.Instance(this).GraphicsBackend;
            if (preferredBackend.HasValue) _preferredBackend = preferredBackend.Value;

            //_logger = LogManager.GetLogger<SimpleViewer>();

            // Create Subjects
            _viewerInputEvents = new Subject<IEvent>();

            // TODO - Remove?
            //InputEvents = new Subject<IUiEventAdapter>();

            _windowTitle = title;

            var wci = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = title
            };

            _window = VeldridStartup.CreateWindow(ref wci);
            DisplaySettings.Instance(this).SetScreenWidth((uint) wci.WindowWidth);
            DisplaySettings.Instance(this).SetScreenHeight((uint) wci.WindowHeight);
            DisplaySettings.Instance(this).SetScreenDistance(1000.0f);

            //
            // This is a "trick" to get continuous resize behavior
            // On Windows.  This should probably be integrated into
            // Veldrid.SDL2
            //
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ResizeEventFilter = ResizingEventWatcher;
                SDL_AddEventWatch(ResizeEventFilter, null);
            }

            _window.Resized += () =>
            {
                _windowResized = true;
                Frame();
            };

            _sceneContext = new SceneContext(fsaaCount);
            _window.KeyDown += OnKeyDown;
            GraphicsDeviceOperations += Camera.Renderer.HandleOperation;
            GraphicsDeviceResize += Camera.Renderer.HandleResize;
            InputEvents = _viewerInputEvents;
            SceneContext = _sceneContext;

            Camera.SetProjectionResizePolicy(ProjectionResizePolicy.Fixed);

            AddInputEventHandler(new FrameCaptureEventHandler());
        }

        public ResourceFactory ResourceFactory => _factory;
        public GraphicsDevice GraphicsDevice { get; private set; }

        public GraphicsBackend Backend => GraphicsDevice.ResourceFactory.BackendType;

        private InputSnapshotAdapter InputSnapshotAdapter { get; } = new InputSnapshotAdapter();
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        //
        // PUBLIC Properties
        //
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public uint Width => (uint) _window.Width;
        public uint Height => (uint) _window.Height;
        public Platform PlatformType { get; }

        public override void SetCamera(ICamera camera)
        {
            GraphicsDeviceOperations -= Camera.Renderer.HandleOperation;
            GraphicsDeviceResize -= Camera.Renderer.HandleResize;

            base.SetCamera(camera);

            GraphicsDeviceOperations += Camera.Renderer.HandleOperation;
            GraphicsDeviceResize += Camera.Renderer.HandleResize;
        }

        public void SetCameraOrthographic()
        {
            var camera = OrthographicCameraOperations.CreateOrthographicCamera(
                DisplaySettings.Instance(this).ScreenWidth,
                DisplaySettings.Instance(this).ScreenHeight,
                DisplaySettings.Instance(this).ScreenDistance);
            SetCamera(camera);
        }

        public void SetCameraPerspective()
        {
            var camera = PerspectiveCameraOperations.CreatePerspectiveCamera(
                DisplaySettings.Instance(this).ScreenWidth,
                DisplaySettings.Instance(this).ScreenHeight,
                DisplaySettings.Instance(this).ScreenDistance);
            SetCamera(camera);
        }

        public void ViewAll()
        {
            CameraManipulator?.ViewAll(this);
        }


        public void SetBackgroundColor(RgbaFloat color)
        {
            // TODO - fix this nasty cast
            Camera.SetClearColor(color);
        }

        /// <summary>
        ///     Run the viewer
        /// </summary>
        /// <param name="preferredBackend"></param>
        //
        // TODO: This runs continuously, probably should have a mode that runs one-frame-at-a-time.
        // 
        public void Run()
        {
            _windowTitle = _windowTitle + " (" + _preferredBackend + ") ";

            while (_window.Exists)
            {
                var inputSnapshot = _window.PumpEvents();

                var events = InputSnapshotAdapter.Adapt(inputSnapshot, _window.Width, _window.Height);
                foreach (var evt in events) _viewerInputEvents.OnNext(evt);

                Frame();
            }

            //_viewerInputEvents.OnCompleted();

            DisposeResources();
        }

        public override void RequestRedraw()
        {
            base.RequestRedraw();
            Frame();
        }

        private event Action<GraphicsDevice, ResourceFactory> GraphicsDeviceOperations;
        private event Action<GraphicsDevice> GraphicsDeviceResize;

        //private ILog _logger;


        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        //
        // PUBLIC Methods
        //
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public static IViewer Create(string title, TextureSampleCount fsaaCount = TextureSampleCount.Count1,
            GraphicsBackend? preferredBackend = null)
        {
            return new SimpleViewer(title, fsaaCount, preferredBackend);
        }

        public ICamera GetCamera()
        {
            return Camera;
        }

        public void Home()
        {
            CameraManipulator?.Home(this);
        }

        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        //
        // PROTECTED Methods
        //
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        //
        // Dispose Properly
        // 
        protected void DisposeResources()
        {
            GraphicsDevice.WaitForIdle();
            _factory.DisposeCollector.DisposeAll();
            GraphicsDevice.Dispose();
            GraphicsDevice = null;
        }

        // 
        // Invoke Keyboard events.
        //
        protected void OnKeyDown(KeyEvent keyEvent)
        {
            //KeyPressed?.Invoke(keyEvent);
        }

        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        //
        // PRIVATE Methods
        //
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        //
        // This is a "trick" to get continuous resize behavior
        // On Windows.  This should probably be integrated into
        // Veldrid.SDL2
        //
        private unsafe int ResizingEventWatcher(void* data, SDL_Event* @event)
        {
            if (@event->type != SDL_EventType.WindowEvent) return 0;

            var windowEvent = Unsafe.Read<SDL_WindowEvent>(@event);
            if (windowEvent.@event == SDL_WindowEventID.Resized) _window.PumpEvents();

            return 0;
        }


        // 
        // Initialize the viewer
        //
        private void ViewerInit()
        {
            var options = new GraphicsDeviceOptions(
                false,
                PixelFormat.R32_Float,
                true,
                ResourceBindingModel.Improved,
                true,
                true,
                false);

#if DEBUG
            options.Debug = true;
#endif
            //_logger.Info(m => m($"Creating Graphics Device with {_preferredBackend} Backend"));

            GraphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, options, _preferredBackend);

            var isDirect3DSupported = GraphicsDevice.IsBackendSupported(GraphicsBackend.Direct3D11);
            _factory = new DisposeCollectorResourceFactory(GraphicsDevice.ResourceFactory);
            _stopwatch = Stopwatch.StartNew();
            _previousElapsed = _stopwatch.Elapsed.TotalSeconds;

            _sceneContext.SetOutputFramebufffer(GraphicsDevice.SwapchainFramebuffer);
            _sceneContext.CreateDeviceObjects(GraphicsDevice, _factory);
        }

        // 
        // Draw a frame
        // 
        private void Frame()
        {
            if (_firstFrame)
            {
                ViewerInit();
                _firstFrame = false;
            }

            if (!_window.Exists) return;

            var newElapsed = _stopwatch.Elapsed.TotalSeconds;
            var deltaSeconds = (float) (newElapsed - _previousElapsed);

            //
            // Rudimentary FPS Calc
            // 
            {
                _frameTimeAccumulator -= _frameTimeBuff[_frameCounter];
                _frameTimeBuff[_frameCounter] = deltaSeconds;
                _frameTimeAccumulator += deltaSeconds;

                _fpsDrawTimeAccumulator += deltaSeconds;
                if (_fpsDrawTimeAccumulator > 0.03333)
                {
                    var avgFps = NFramesInBuffer / _frameTimeAccumulator;

                    _window.Title = _windowTitle + ": FPS: " + avgFps.ToString("#.0");
                    _fpsDrawTimeAccumulator = 0.0;
                }

                // RingBuffer
                if (_frameCounter == NFramesInBuffer - 1)
                    _frameCounter = 0;
                else
                    _frameCounter++;
            }

            _globalFrameCounter++;
            _previousElapsed = newElapsed;

            if (null == GraphicsDevice) return;

            RenderingTraversals();
        }

        //
        // Run the traversals.
        //
        private void RenderingTraversals()
        {
            if (_windowResized)
            {
                _windowResized = false;
                GraphicsDevice.ResizeMainWindow((uint) _window.Width, (uint) _window.Height);
                DisplaySettings.Instance(this).SetScreenWidth((uint) _window.Width);
                DisplaySettings.Instance(this).SetScreenHeight((uint) _window.Height);

                Camera.Resize(
                    _window.Width,
                    _window.Height,
                    ResizeMask.ResizeDefault | ResizeMask.ResizeProjectionMatrix);

                _sceneContext.SetOutputFramebufffer(GraphicsDevice.SwapchainFramebuffer);

                _sceneContext.RecreateWindowSizedResources(
                    GraphicsDevice,
                    _factory);

                GraphicsDeviceResize?.Invoke(GraphicsDevice);
            }

            GraphicsDeviceOperations?.Invoke(GraphicsDevice, _factory);
        }
    }
}