//
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

using Veldrid.SceneGraph.AssetPrimitives;

namespace Veldrid.SceneGraph
{
    public abstract class TextureBase : ITexture
    {
        public ProcessedTexture ProcessedTexture { get; protected set; }
        
        public SamplerDescription SamplerDescription { get; protected set; }
        
        public uint ResourceSetNo { get; set; }
        public string TextureName { get; set; }
        public string SamplerName { get; set; }

        protected TextureBase(uint resourceSetNo, string textureName, string samplerName)
        {
            SamplerDescription = SamplerDescription.Aniso4x;
            ResourceSetNo = resourceSetNo;
            TextureName = textureName;
            SamplerName = samplerName;
            
        }
        
        protected TextureBase(ProcessedTexture processedTexture, SamplerDescription samplerDescription, uint resourceSetNo, string textureName, string samplerName)
        {
            ResourceSetNo = resourceSetNo;
            TextureName = textureName;
            SamplerName = samplerName;
            ProcessedTexture = processedTexture;
        }
    }
}