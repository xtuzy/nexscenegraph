﻿using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Serilog;
using Veldrid;
using Veldrid.SceneGraph;
using Veldrid.SceneGraph.InputAdapter;
using Veldrid.SceneGraph.Manipulators;
using Veldrid.SceneGraph.Manipulators.Commands;
using Veldrid.SceneGraph.Math.IsoSurface;
using Veldrid.SceneGraph.NodeKits.DirectVolumeRendering;
using Veldrid.SceneGraph.Shaders;
using Veldrid.SceneGraph.Util;
using EventHandler = System.EventHandler;
using Math = System.Math;

namespace Examples.Common
{
    public class VoxelVolume : IVoxelVolume
    {
        protected int XDim = 2;
        protected int YDim = 2;
        protected int ZDim = 2;


        public VoxelVolume()
        {
            Values = new double[XDim, YDim, ZDim];
            XValues = new double[XDim, YDim, ZDim];
            YValues = new double[XDim, YDim, ZDim];
            ZValues = new double[XDim, YDim, ZDim];
        }

        public double[,,] Values { get; }
        public double[,,] XValues { get; }
        public double[,,] YValues { get; }
        public double[,,] ZValues { get; }
    }

    public class CornerVoxelVolume : VoxelVolume
    {
        public CornerVoxelVolume()
        {
            for (var z = 0; z < ZDim; ++z)
            for (var y = 0; y < YDim; ++y)
            for (var x = 0; x < XDim; ++x)
            {
                XValues[x, y, z] = x;
                YValues[x, y, z] = y;
                ZValues[x, y, z] = z;
                Values[x, y, z] = 0;
            }

            //
            // Values[0, 0, 0] = 1.0;
            // Values[1, 1, 1] = 1.0;
        }
    }

    public class DraggerVolumeTileCallback : DraggerCallback
    {
        //private Matrix4x4 _localToWorld;
        private readonly ILocator _locator;

        //private Matrix4x4 _startMotionMatrix;
        private readonly IVolumeTile _volumeTile;
        //private Matrix4x4 _worldToLocal;

        private ITabBoxDragger _dragger;
        
        public DraggerVolumeTileCallback(IVolumeTile volumeTile, ILocator locator, ITabBoxDragger dragger)
        {
            _volumeTile = volumeTile;
            _locator = locator;
            _dragger = dragger;
        }

        public override bool Receive(IMotionCommand command)
        {
            if (null == _locator) return false;
            
            switch (command.Stage)
            {
                case IMotionCommand.MotionStage.Start:
                {
                    return true;
                }
                case IMotionCommand.MotionStage.Move:
                {
                    _locator.SetTransform(_dragger.Matrix);
                    
                    return true;
                }
                case IMotionCommand.MotionStage.Finish:
                {
                    return true;
                }
                case IMotionCommand.MotionStage.None:
                default:
                    return false;
            }
        }
    }

    public class RotateDraggerEventHandler : UiEventHandler
    {
        //private readonly ILogger _logger;
        private ITabBoxDragger _dragger;
        private ILevoyCabralLocator _locator;

        private int zDegrees = 0;
        private int yDegrees = 0;
        private int xDegrees = 0;
        
        public RotateDraggerEventHandler(ITabBoxDragger dragger, ILevoyCabralLocator locator)
        {
            _dragger = dragger;
            _locator = locator;
        }

        public override bool Handle(IUiEventAdapter eventAdapter, IUiActionAdapter uiActionAdapter)
        {
            var unitToWorld = _locator.Transform;
            var zeroInWorld = Vector3.Transform(Vector3.Zero, _locator.Transform);
            var translateToZero = Matrix4x4.CreateTranslation(zeroInWorld);

            var doRot = false;
            
            switch (eventAdapter.Key)
            {
                case IUiEventAdapter.KeySymbol.KeyX:
                    if ((eventAdapter.ModKeyMask & IUiEventAdapter.ModKeyMaskType.ModKeyShift) != 0)
                    {
                        xDegrees -= 10;
                    }
                    else
                    {
                        xDegrees += 10;
                    }

                    doRot = true;
                    break;
                
                case IUiEventAdapter.KeySymbol.KeyY:
                    if ((eventAdapter.ModKeyMask & IUiEventAdapter.ModKeyMaskType.ModKeyShift) != 0)
                    {
                        yDegrees -= 10;
                    }
                    else
                    {
                        yDegrees += 10;
                    }
                    doRot = true;
                    break;
                
                case IUiEventAdapter.KeySymbol.KeyZ:
                    if ((eventAdapter.ModKeyMask & IUiEventAdapter.ModKeyMaskType.ModKeyShift) != 0)
                    {
                        zDegrees -= 10;
                    }
                    else
                    {
                        zDegrees += 10;
                    }
                    doRot = true;
                    break;
                default:
                    break;
            }

            if (doRot)
            {
                var rot =
                    Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, (float) (xDegrees * Math.PI / 180)) *
                    Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, (float) (yDegrees * Math.PI / 180)) *
                    Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, (float) (zDegrees * Math.PI / 180));
            
                if (Matrix4x4.Invert(translateToZero, out var translateFromZero))
                {
                    _dragger.Rotation = rot;
                    Matrix4x4.Decompose(_dragger.Matrix, out var scale, out var curRot, out var translation);
                    var inverseRotation = Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(curRot));
                        
                        
                    _dragger.Matrix = unitToWorld * translateFromZero * inverseRotation*rot * translateToZero;
                        
                }

                _locator.SetTransform(_dragger.Matrix);
            
                return true;
            }

            return false;
        }
    }
    
    public class VolumeExtentsConstraint : Constraint
    {
        private float _xmax;
        private float _xmin;
        private float _ymax;
        private float _ymin;
        private float _zmax;
        private float _zmin;

        public VolumeExtentsConstraint(ILevoyCabralLocator baseLocator)
        {
            _xmin = (float) baseLocator.XMin;
            _xmax = (float) baseLocator.XMax;
            _ymin = (float) baseLocator.YMin;
            _ymax = (float) baseLocator.YMax;
            _zmin = (float) baseLocator.ZMin;
            _zmax = (float) baseLocator.ZMax;
        }

        public override bool Constrain(IMotionCommand command)
        {
            return base.Constrain(command);
        }
    }
    
    internal class UpdateVolumeColormapCallback : NodeCallback, INodeCallback
    {
        private ILevoyCabralTechnique _technique;
        private DateTime _lastUpdate;
        private DateTime _startTime;
        
        internal UpdateVolumeColormapCallback(ILevoyCabralTechnique technique)
        {
            _technique = technique;
            _lastUpdate = DateTime.Now;
            _startTime = DateTime.Now;
        }

        private static byte[] GenerateColormap(uint colormapSize, long ticks)
        {
            var random = new Random();
            var rgbaData = new byte[colormapSize * 4];

            for (var i = 0; i < colormapSize; ++i)
            {
                var val = i / ((float) colormapSize - 1);

                var index = 4 * i;

                rgbaData[index + 0] = (byte) random.Next(127,255); // R
                rgbaData[index + 1] = (byte) random.Next(127,255);; // G
                rgbaData[index + 2] = (byte) random.Next(127,255);; // B
                rgbaData[index + 3] = 255; // A

                // rgbaData[index] = 0x10000FF;  // RGBA ... A is the 4th component ... solid blue
                if (val > 0.9)
                {
                    rgbaData[index + 0] = (byte) random.Next(127,255);; // R
                    rgbaData[index + 1] = (byte) random.Next(127,255);; // G
                    rgbaData[index + 2] = (byte) random.Next(127,255);; // B
                    
                    
                    rgbaData[index + 3] = 255; // A
                }
                else if (val < 0.1)
                {
                    // rgbaData[index] = 0xFFFF0000;  // RGBA ... A is the 4th component ... solid blue
                    rgbaData[index + 0] = (byte) random.Next(127,255);;
                    rgbaData[index + 1] = (byte) random.Next(127,255);;
                    rgbaData[index + 2] = (byte) random.Next(127,255);;
                    
                    var alpha = (byte)(ticks % 128);
                    rgbaData[index + 3] = alpha;
                }
            }

            return rgbaData;
        }

        public override bool Run(IObject obj, IObject data)
        {
            var curTime = DateTime.Now;
            var span = curTime - _lastUpdate;
            var totalSecs = (long)System.Math.Ceiling((curTime - _startTime).TotalMilliseconds/20);
            if (span.TotalSeconds > 1)
            {
                var textureData = _technique.ColormapData?.ProcessedTexture?.TextureData;
                if (null != textureData)
                {
                    var colormapSize = (uint)(textureData.Length / 4);
                    var newData = GenerateColormap(colormapSize, totalSecs);

                    _technique.ColormapData.ProcessedTexture.TextureData = newData;
                    _technique.ColormapData.Dirty();
                }

                _lastUpdate = curTime;
            }
            else
            {
                var rgbaData = _technique.ColormapData?.ProcessedTexture?.TextureData.ToArray();
                if (null != rgbaData)
                {
                    var colormapSize = (uint) (rgbaData.Length / 4);
                    
                    for (var i = 0; i < colormapSize; ++i)
                    {
                        var val = i / ((float) colormapSize - 1);

                        var index = 4 * i;

                        if (val < 0.1)
                        {
                            var alpha = (byte)(totalSecs % 128);
                            rgbaData[index + 3] = alpha;
                        }
                    }
                    _technique.ColormapData.ProcessedTexture.TextureData = rgbaData;
                    _technique.ColormapData.Dirty();
                }
                
            }
            
            
            return true;
        }
    }

    

    public class SampledVolumeRenderingExampleScene
    {
        public delegate IShaderSet ShaderSetBuilder();

        public static (IGroup, IUiEventHandler) Build(ShaderSetBuilder builder, IVoxelVolume voxelVolume,
            LevoyCabralTechnique.VolumeTextureGenerator volumeTextureGenerator,
            LevoyCabralTechnique.ColormapTextureGenerator colormapTextureGenerator)
        {
            
            var volume = Volume.Create();
            var tile1 = VolumeTile.Create();
            
            volume.AddChild(tile1);

            var layer = VoxelVolumeLayer.Create(voxelVolume);
            tile1.Layer = layer;
            tile1.Locator = layer.GetLocator();
            var technique = LevoyCabralTechnique.Create(builder(), volumeTextureGenerator, colormapTextureGenerator);
            technique.SetSampleRate(20);
            technique.SetDrawOutlines(true);
            tile1.SetVolumeTechnique(technique);
            
            //tile1.SetUpdateCallback(new UpdateVolumeColormapCallback(technique));

            var dragger1 = TabBoxDragger.Create();
            dragger1.SetupDefaultGeometry();
            dragger1.ActivationModKeyMask = IUiEventAdapter.ModKeyMaskType.ModKeyCtl;
            dragger1.HandleEvents = true;
            dragger1.DraggerCallbacks.Add(new DraggerVolumeTileCallback(tile1, tile1.Locator, dragger1));
            dragger1.Matrix =
                Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f) *
                    tile1.Locator.Transform;
            
            tile1.Locator.SetTransform(dragger1.Matrix);

            // dragger1.Matrix =
            //     Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f)
            //         .PostMultiply(tile1.Locator.Transform);
            
            /////////////////

            // var tile2 = VolumeTile.Create();
            // tile2.Layer = layer;
            // tile2.Locator = layer.GetLocator();
            // tile2.SetVolumeTechnique(LevoyCabralTechnique.Create(builder(), textureTranslator));
            // volume.AddChild(tile2);
            //
            // var dragger2 = TabBoxDragger.Create();
            // dragger2.SetupDefaultGeometry();
            // dragger2.ActivationModKeyMask = IUiEventAdapter.ModKeyMaskType.ModKeyAlt;
            // dragger2.HandleEvents = true;
            // dragger2.DraggerCallbacks.Add(new DraggerVolumeTileCallback(tile2, tile2.Locator));
            // dragger2.Matrix = Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f)
            //     .PostMultiply(tile2.Locator.Transform);

            /////////////////

            var root = Group.Create();
            root.AddChild(volume);

            
            root.AddChild(dragger1);

            
            var eventHandler = new RotateDraggerEventHandler(dragger1, (ILevoyCabralLocator)tile1.Locator);
            
            //root.AddChild(dragger2);
            return (root, eventHandler);
        }

        public static IGroup Build()
        {
            var (root, handler) = Build(CreateShaderSet, new CornerVoxelVolume(), null, null);
            return root;
        }

        public static IShaderSet CreateShaderSet()
        {
            var asm = Assembly.GetCallingAssembly();

            var vtxShader =
                new ShaderDescription(ShaderStages.Vertex,
                    ShaderTools.ReadEmbeddedAssetBytes(
                        @"Examples.Common.Assets.Shaders.ProceduralVolumeShader-vertex.glsl", asm), "main");

            var frgShader =
                new ShaderDescription(ShaderStages.Fragment,
                    ShaderTools.ReadEmbeddedAssetBytes(
                        @"Examples.Common.Assets.Shaders.ProceduralVolumeShader-fragment.glsl", asm), "main");

            return ShaderSet.Create("ProceduralVolumeShader", vtxShader, frgShader);
        }
    }
}