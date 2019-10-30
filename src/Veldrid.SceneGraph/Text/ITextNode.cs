﻿//
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

using SixLabors.Fonts;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;

namespace Veldrid.SceneGraph.Text
{
    public enum CharacterSizeModes
    {
        ObjectCoords,
        ScreenCoords
    }
    
    public interface ITextNode : IGeometry<VertexPositionTexture>
    {
        string Text { get; }
        
        int Padding { get; }
        
        float FontResolution { get; }
        
        Rgba32 TextColor { get; }
        
        Rgba32 BackgroundColor { get; }
        
        VerticalAlignment VerticalAlignment { get; }
        
        HorizontalAlignment HorizontalAlignment { get; }
        
        bool AutoRotateToScreen { get; set; }
        
        CharacterSizeModes CharacterSizeMode { get; set; }
    }
}