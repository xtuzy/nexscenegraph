//
// Copyright 2018 Sean Spicer 
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

namespace Veldrid.SceneGraph
{
    public interface IGeode : INode
    {
        IReadOnlyList<IDrawable> Drawables { get; }
        IBoundingBox GetBoundingBox();
        event Func<INode, IBoundingBox> ComputeBoundingBoxCallback;
        void AddDrawable(IDrawable drawable);
    }
}