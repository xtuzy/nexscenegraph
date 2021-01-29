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

namespace Veldrid.SceneGraph.Util
{
    public interface IIntersector
    {
        enum CoordinateFrameMode
        {
            Window, 
            Projection,
            View, 
            Model
        }
        
        enum IntersectionLimitModes
        {
            NoLimit,
            LimitOnePerDrawable,
            LimitOne,
            LimitNearest
        };
        
        IntersectionLimitModes IntersectionLimit { get; }
        CoordinateFrameMode CoordinateFrame { get; }
        IIntersector Clone(IIntersectionVisitor iv);
        void Intersect(IIntersectionVisitor iv, IDrawable drawable);
        bool Enter(INode node);
        void Leave();
        void Reset();
    }
    
    /// <summary>
    /// Base class for all intersectors
    /// </summary>
    public abstract class Intersector : IIntersector
    {
        public IIntersector.IntersectionLimitModes IntersectionLimit { get; protected set; }

        public IIntersector.CoordinateFrameMode CoordinateFrame { get; protected set; } 
        
        protected Intersector(IIntersector.CoordinateFrameMode coordinateFrame = IIntersector.CoordinateFrameMode.Model,
            IIntersector.IntersectionLimitModes intersectionLimit=IIntersector.IntersectionLimitModes.NoLimit)
        {
            CoordinateFrame = coordinateFrame;
            IntersectionLimit = intersectionLimit;
        }
        
        public abstract IIntersector Clone(IIntersectionVisitor iv);
        
        public abstract void Intersect(IIntersectionVisitor iv, IDrawable drawable);

        public abstract bool Enter(INode node);

        public abstract void Leave();

        public abstract void Reset();

    }
}