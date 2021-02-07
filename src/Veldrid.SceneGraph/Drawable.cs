//
// Copyright 2018-2019 Sean Spicer 
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
using System.Collections.Generic;
using System.Numerics;

namespace Veldrid.SceneGraph
{
    public interface IDrawable : INode
    {
        string Name { get; set; }
        Type VertexType { get; }
        IBoundingBox InitialBoundingBox { get; set; }
        List<VertexLayoutDescription> VertexLayouts { get; set; }
        List<IPrimitiveSet> PrimitiveSets { get; }
        void ConfigureDeviceBuffers(GraphicsDevice device, ResourceFactory factory);
        List<DeviceBuffer> GetVertexBufferForDevice(GraphicsDevice device);
        DeviceBuffer GetIndexBufferForDevice(GraphicsDevice device);
        IBoundingBox GetBoundingBox();
        bool ComputeMatrix(ref Matrix4x4 computedMatrix, IState state);
        public void UpdateDeviceBuffers(GraphicsDevice device);
        bool Supports(IPrimitiveFunctor functor);
        void Accept(IPrimitiveFunctor functor);
        bool Supports(IPrimitiveIndexFunctor functor);
        void Accept(IPrimitiveIndexFunctor functor);
    }
    
    public abstract class Drawable : Node, IDrawable
    {
        public string Name { get; set; } = string.Empty;

        public Type VertexType => GetVertexType();
        
        private IBoundingBox _boundingBox;
        private IBoundingBox _initialBoundingBox = BoundingBox.Create();
        public IBoundingBox InitialBoundingBox
        {
            get => _initialBoundingBox;
            set
            {
                _initialBoundingBox = value;
                DirtyBound();
            }
        }

        private IBoundingBox _fixedBoundingBox = null;
        public void SetFixedBoundingBox(IBoundingBox boundingBox)
        {
            _fixedBoundingBox = boundingBox;
        }

        public override void Accept(INodeVisitor nv)
        {
            if (nv.ValidNodeMask(this))
            {
                nv.PushOntoNodePath(this);
                nv.Apply(this);
                nv.PopFromNodePath(this);
            };
        }
        
        public List<VertexLayoutDescription> VertexLayouts { get; set; }
        
        public List<IPrimitiveSet> PrimitiveSets { get; } = new List<IPrimitiveSet>();
        
        public event Func<Drawable, BoundingBox> ComputeBoundingBoxCallback;
        public event Action<CommandList, Drawable> DrawImplementationCallback;
        
        protected abstract Type GetVertexType();
        
        public void Draw(GraphicsDevice device, List<Tuple<uint, ResourceSet>> resourceSets, CommandList commandList)
        {
            if (null != DrawImplementationCallback)
            {
                DrawImplementationCallback(commandList, this);
            }
            else
            {
                DrawImplementation(device, resourceSets, commandList);
            }
        }

        protected abstract void DrawImplementation(GraphicsDevice device, List<Tuple<uint, ResourceSet>> resourceSets, CommandList commandList);

        public virtual void ConfigureDeviceBuffers(GraphicsDevice device, ResourceFactory factory)
        {
            // Nothing by default
        }

        public virtual void ConfigurePipelinesForDevice(GraphicsDevice device, ResourceFactory factory,
            ResourceLayout parentLayout)
        {
            // Nothing by default
        }     
        
        public IBoundingBox GetBoundingBox()
        {
            if (_boundingSphereComputed) return _boundingBox;
            
            _boundingBox = _initialBoundingBox;

            _boundingBox.ExpandBy(null != ComputeBoundingBoxCallback
                ? ComputeBoundingBoxCallback(this)
                : ComputeBoundingBox());

            if (_boundingBox.Valid())
            {
                _boundingSphere.Set(_boundingBox.Center, _boundingBox.Radius);
            }
            else
            {
                _boundingSphere.Init();
            }

            _boundingSphereComputed = true;

            return _boundingBox;
        }

        public virtual bool ComputeMatrix(ref Matrix4x4 computedMatrix, IState state)
        {
            return false;
        }

        protected abstract IBoundingBox ComputeBoundingBox();

        public abstract List<DeviceBuffer> GetVertexBufferForDevice(GraphicsDevice device);

        public abstract DeviceBuffer GetIndexBufferForDevice(GraphicsDevice device);

        public abstract void UpdateDeviceBuffers(GraphicsDevice device);

        public virtual bool Supports(IPrimitiveFunctor functor) { return false; }
        public virtual void Accept(IPrimitiveFunctor functor) {}
        public virtual bool Supports(IPrimitiveIndexFunctor functor) { return false; }
        public virtual void Accept(IPrimitiveIndexFunctor functor) {}
    }
}