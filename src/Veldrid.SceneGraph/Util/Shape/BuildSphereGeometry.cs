﻿//
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
using System.Numerics;
using SixLabors.ImageSharp.ColorSpaces;
using Veldrid.SceneGraph.VertexTypes;

namespace Veldrid.SceneGraph.Util.Shape
{
    internal class BuildSphereGeometry<T> where T : struct, ISettablePrimitiveElement
    {
        private class QuadStrip
        {
            public List<Vector3> Vertices = new List<Vector3>();
            public List<Vector3> Normals = new List<Vector3>();
            public List<Vector2> TexCoords = new List<Vector2>();
        }
        
        const uint MIN_NUM_ROWS = 3;
        const uint MIN_NUM_SEGMENTS = 5;

        private List<QuadStrip> _strips = new List<QuadStrip>();
        private QuadStrip _currentStrip;

        private void Begin()
        {
            _currentStrip = new QuadStrip();
        }

        private void End()
        {
            _strips.Add(_currentStrip);
        }

        private void Normal3f(float x, float y, float z)
        {
            _currentStrip.Normals.Add(new Vector3(x, y, z));
        }

        private void Vertex3f(float x, float y, float z)
        {
            _currentStrip.Vertices.Add(new Vector3(x, y, z));
        }

        private void TexCoord2f(float x, float y)
        {
            _currentStrip.TexCoords.Add(new Vector2(x, y));
        }
        
        internal void Build(IGeometry<T> geometry, ITessellationHints hints, Vector3[] colors, ISphere sphere)
        {
            if (hints.NormalsType == NormalsType.PerFace)
            {
                throw new ArgumentException("Per-Face Normals are not supported for spheres");
            }

            if (hints.ColorsType == ColorsType.ColorPerFace)
            {
                throw new ArgumentException("Per-Face Colors are not supported for spheres");
            }
            
            if (hints.ColorsType == ColorsType.ColorPerVertex)
            {
                throw new ArgumentException("Per-Vertex Colors are not supported for spheres");
            }

            if (colors.Length < 1)
            {
                throw new ArgumentException("Must provide at least one color for spheres");
            }
            
            uint numSegments = 40;
            uint numRows = 20;

            var ratio = hints.DetailRatio;
            if (ratio > 0.0f && ratio != 1.0f) {
                numRows = (uint) (numRows * ratio);
                if (numRows < MIN_NUM_ROWS)
                    numRows = MIN_NUM_ROWS;
                numSegments = (uint) (numSegments * ratio);
                if (numSegments < MIN_NUM_SEGMENTS)
                    numSegments = MIN_NUM_SEGMENTS;
            }

            var lDelta = (float)System.Math.PI/(float)numRows;
            var vDelta = 1.0f/(float)numRows;

            var angleDelta = (float)System.Math.PI*2.0f/numSegments;
            var texCoordHorzDelta = 1.0f/numSegments;
            
            if (hints.CreateBackFace)
            {
                var lBase=-(float)System.Math.PI*0.5f;
                var rBase=0.0f;
                var zBase=-sphere.Radius;
                var vBase=0.0f;
                var nzBase=-1.0f;
                var nRatioBase=0.0f;

                for(uint rowi=0; rowi<numRows; ++rowi)
                {
                    var lTop = (float)(lBase+lDelta);
                    var rTop = (float)System.Math.Cos(lTop)*sphere.Radius;
                    var zTop = (float)System.Math.Sin(lTop)*sphere.Radius;
                    var vTop = vBase+vDelta;
                    var nzTop= (float)System.Math.Sin(lTop);
                    var nRatioTop= (float)System.Math.Cos(lTop);

                    Begin();

                    var angle = 0.0f;
                    var texCoord = 0.0f;

                    for(uint topi=0; topi<numSegments;
                    ++topi,angle+=angleDelta,texCoord+=texCoordHorzDelta)
                    {
                        var c = (float)System.Math.Cos(angle);
                        var s = (float)System.Math.Sin(angle);

                        Normal3f(-c*nRatioBase,-s*nRatioBase,-nzBase);
                        TexCoord2f(texCoord,vBase);
                        Vertex3f(c*rBase,s*rBase,zBase);

                        Normal3f(-c*nRatioTop,-s*nRatioTop,-nzTop);

                        TexCoord2f(texCoord,vTop);
                        Vertex3f(c*rTop,s*rTop,zTop);
                    }
                    
                    // do last point by hand to ensure no round off errors.
                    Normal3f(-nRatioBase,0.0f,-nzBase);
                    TexCoord2f(1.0f,vBase);
                    Vertex3f(rBase,0.0f,zBase);

                    Normal3f(-nRatioTop,0.0f,-nzTop);
                    TexCoord2f(1.0f,vTop);
                    Vertex3f(rTop,0.0f,zTop);

                    End();
                    
                    lBase=lTop;
                    rBase=rTop;
                    zBase=zTop;
                    vBase=vTop;
                    nzBase=nzTop;
                    nRatioBase=nRatioTop;

                }
            }
            if (hints.CreateFrontFace)
            {
                var lBase=-(float)System.Math.PI*0.5f;
                var rBase=0.0f;
                var zBase=-sphere.Radius;
                var vBase=0.0f;
                var nzBase=-1.0f;
                var nRatioBase=0.0f;

                for(uint rowi=0; rowi<numRows; ++rowi)
                {
                    var lTop = lBase+lDelta;
                    var rTop = (float)System.Math.Cos(lTop)*sphere.Radius;
                    var zTop = (float)System.Math.Sin(lTop)*sphere.Radius;
                    var vTop = vBase+vDelta;
                    var nzTop= (float)System.Math.Sin(lTop);
                    var nRatioTop= (float)System.Math.Cos(lTop);

                    Begin();

                    var angle = 0.0f;
                    var texCoord = 0.0f;

                    for(uint topi=0; topi<numSegments;
                    ++topi,angle+=angleDelta,texCoord+=texCoordHorzDelta)
                    {

                        float c = (float)System.Math.Cos(angle);
                        float s = (float)System.Math.Sin(angle);

                        Normal3f(c*nRatioTop,s*nRatioTop,nzTop);
                        TexCoord2f(texCoord,vTop);
                        Vertex3f(c*rTop,s*rTop,zTop);

                        Normal3f(c*nRatioBase,s*nRatioBase,nzBase);
                        TexCoord2f(texCoord,vBase);
                        Vertex3f(c*rBase,s*rBase,zBase);
                    }

                    // do last point by hand to ensure no round off errors.
                    Normal3f(nRatioTop,0.0f,nzTop);
                    TexCoord2f(1.0f,vTop);
                    Vertex3f(rTop,0.0f,zTop);

                    Normal3f(nRatioBase,0.0f,nzBase);
                    TexCoord2f(1.0f,vBase);
                    Vertex3f(rBase,0.0f,zBase);

                    End();
                    
                    lBase=lTop;
                    rBase=rTop;
                    zBase=zTop;
                    vBase=vTop;
                    nzBase=nzTop;
                    nRatioBase=nRatioTop;

                }
            }
            
            var vertexDataList = new List<T>();
            var indexDataList = new List<uint>();
            var lastIdx = 0;
            foreach (var strip in _strips)
            {
                var nQuads = (uint)(strip.Vertices.Count / 2 - 1);

                // Convert QuadStrips to Triangle List
                var triIndicies = new List<int>();
                for (var qidx = 0; qidx < nQuads; ++qidx)
                {
                    var q = qidx * 2;
                    
                    triIndicies.AddRange(new int[] {q, q+3, q+1});
                    triIndicies.AddRange(new int[] {q, q+2, q+3});
                }

                foreach (var idx in triIndicies)
                {
                    indexDataList.Add((uint)(lastIdx+idx));
                }
                lastIdx += strip.Vertices.Count;
                for (var idx = 0; idx < strip.Vertices.Count; ++idx)
                {
                    var vtx = new T();
                    vtx.SetPosition(strip.Vertices[idx]);
                    vtx.SetNormal(strip.Normals[idx]);
                    vtx.SetTexCoord(strip.TexCoords[idx]);
                    vtx.SetColor3(colors[0]);
                    vertexDataList.Add(vtx);
                }
            }
            
            geometry.VertexData = vertexDataList.ToArray();
            geometry.IndexData = indexDataList.ToArray();

            geometry.VertexLayout = VertexLayoutHelpers.GetLayoutDescription(typeof(T));
            
            var pSet = DrawElements<T>.Create(
                geometry, 
                PrimitiveTopology.TriangleList, 
                (uint)geometry.IndexData.Length, 
                1, 
                0, 
                0, 
                0);
            
            geometry.PrimitiveSets.Add(pSet);
        }
    }
}