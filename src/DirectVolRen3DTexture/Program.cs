﻿// #define USE_CORNER

using System;
using System.Reflection;
using Examples.Common;
using Veldrid;
using Veldrid.SceneGraph;
using Veldrid.SceneGraph.AssetPrimitives;
using Veldrid.SceneGraph.InputAdapter;
using Veldrid.SceneGraph.Logging;
using Veldrid.SceneGraph.Math.IsoSurface;
using Veldrid.SceneGraph.NodeKits.DirectVolumeRendering;
using Veldrid.SceneGraph.Shaders;
using Veldrid.SceneGraph.Util;
using Veldrid.SceneGraph.Viewer;

namespace DirectVolRen3DTexture
{
    class Program
    {
        private static void Main(string[] args)
        {
            Bootstrapper.Configure();
            LogManager.SetLogger(Bootstrapper.LoggerFactory);

            var viewer = SimpleViewer.Create("Direct Volume Rendering w/ 3D Textures", TextureSampleCount.Count8);
            viewer.SetBackgroundColor(RgbaFloat.Black);
            viewer.SetCameraManipulator(TrackballManipulator.Create());

            var root = SampledVolumeRenderingExampleScene.Build(CreateShaderSet, 
                new TestVoxelVolume(256, 256, 256), Example3DTranslator);

            viewer.SetSceneData(root);
            viewer.ViewAll();
            viewer.Run();
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
                        @"Examples.Common.Assets.Shaders.TextureVolumeShader-fragment.glsl", asm), "main");

            return ShaderSet.Create("TextureVolumeShader", vtxShader, frgShader);
        }

        private static ITexture3D Example3DTranslator(IVoxelVolume voxelVolume)
        {
            var xdim = voxelVolume.XValues.GetLength(0);
            var ydim = voxelVolume.XValues.GetLength(1);
            var zdim = voxelVolume.XValues.GetLength(2);
            
            var allTexData = RgbaData(voxelVolume, xdim, ydim, zdim);
            
            var texData = new ProcessedTexture(
                PixelFormat.R8_G8_B8_A8_UNorm, TextureType.Texture3D,
                (uint) xdim, (uint) ydim, (uint) zdim,
                (uint) 1, 1,
                allTexData);
            
            return Texture3D.Create(texData, 1,
                "SurfaceTexture", "SurfaceSampler");
        }

        private static byte[] RgbaData(IVoxelVolume voxelVolume, int xdim, int ydim, int zdim)
        {
            var rgbaData = new byte[xdim * ydim * zdim * 4];

            for (var x = 0; x < xdim; ++x)
            for (var y = 0; y < ydim; ++y)
            for (var z = 0; z < zdim; ++z)
            {
                var index = (z + ydim * (y + xdim * x)) * 4;
                // rgbaData[index] = 0x10000FF;  // RGBA ... A is the 4th component ... solid blue
                rgbaData[index + 0] = 0xFF; // R
                rgbaData[index + 1] = 0x00; // G
                rgbaData[index + 2] = 0x00; // B
                rgbaData[index + 3] = 0x01; // A
                if (voxelVolume.Values[x, y, z] > 0.6) // inner sphere
                {
                    // rgbaData[index] = 0xFFFF0000;  // RGBA ... A is the 4th component ... solid blue
                    rgbaData[index + 0] = 0x00;
                    rgbaData[index + 1] = 0x00;
                    rgbaData[index + 2] = 0xFF;
                    rgbaData[index + 3] = 0xFF;
                }
                else if (voxelVolume.Values[x, y, z] > 0.1)
                {
                    // rgbaData[index] = 0x1000FFFF;  // RGBA ... A is the 4th component ... transparent yellow
                    rgbaData[index + 0] = 0xFF;
                    rgbaData[index + 1] = 0xFF;
                    rgbaData[index + 2] = 0x00;
                    rgbaData[index + 3] = 0x10;
                }
            }
            return rgbaData;
        }
    }
    
    public class TestVoxelVolume : IVoxelVolume
    {
        public double[,,] Values { get; }
        public double[,,] XValues { get; }
        public double[,,] YValues { get; }
        public double[,,] ZValues { get; }

        public TestVoxelVolume(int width, int height, int depth)
        {

            var centerWidth = width / 2;
            var centerHeight = height / 2;
            var centerDepth = depth / 2;

            var outerRadius = 0.667;
            var innerRadius = 0.333;

            var outerSphereRadiusSq = (width / 2.0) * outerRadius;
            outerSphereRadiusSq *= outerSphereRadiusSq;
            var innerSphereRadiusSq = (width / 2.0) * innerRadius;
            innerSphereRadiusSq *= innerSphereRadiusSq;
            
            Values = new double[width, height, depth];
            XValues = new double[width, height, depth];
            YValues = new double[width, height, depth];
            ZValues = new double[width, height, depth]; // row major
            for (var x = 0; x < width; ++x)
            for (var y = 0; y < height; ++y)
            for (var z = 0; z < depth; ++z)
            {
                XValues[x, y, z] = x;
                YValues[x, y, z] = y;
                ZValues[x, y, z] = z;
                Values[x, y, z] = 0;
                
                var fromCenteri = x - centerWidth;
                var fromCenterj = y - centerHeight;
                var fromCenterk = z - centerDepth;
                if (fromCenteri * fromCenteri + fromCenterj * fromCenterj + fromCenterk * fromCenterk <
                    innerSphereRadiusSq)
                {
                    Values[x, y, z] = 1.0;
                }
                else if (fromCenteri * fromCenteri + fromCenterj * fromCenterj + fromCenterk * fromCenterk <
                         outerSphereRadiusSq)
                {
                    Values[x, y, z] = 0.5;
                }
            }
        }
    }
}