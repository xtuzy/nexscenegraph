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
        VertexLayoutDescription VertexLayout { get; set; }
        List<IPrimitiveSet> PrimitiveSets { get; }
        void ConfigureDeviceBuffers(GraphicsDevice device, ResourceFactory factory);
        DeviceBuffer GetVertexBufferForDevice(GraphicsDevice device);
        DeviceBuffer GetIndexBufferForDevice(GraphicsDevice device);
        IBoundingBox GetBoundingBox();
        bool ComputeMatrix(ref Matrix4x4 computedMatrix, IState state);

    }
}