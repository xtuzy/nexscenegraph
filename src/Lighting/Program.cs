﻿using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using AssetPrimitives;
using Examples.Common;
using SixLabors.ImageSharp;
using Veldrid;
using Veldrid.SceneGraph;
using Veldrid.SceneGraph.InputAdapter;
using Veldrid.SceneGraph.Viewer;
using Veldrid.SceneGraph.IO;

namespace Lighting
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct LightData
    {
        public Vector3 MaterialColor;
        public float LightPower;
        public Vector3 LightColor;
        public float SpecularPower;
        public Vector3 SpecularColor;
        public int MaterialOverride;
        
        public float Padding1;
        public float Padding2;
        public float Padding3;
        public float Padding4;

        public LightData(Vector3 lightColor, float lightPower, Vector3 specularColor, float specularPower,
            Vector3 materialColor, int materialOverride = 0)
        {
            MaterialOverride = materialOverride;
            MaterialColor = materialColor;
            LightColor = lightColor;
            LightPower = lightPower;
            SpecularColor = specularColor;
            SpecularPower = specularPower;

            Padding1 = 0f;
            Padding2 = 0f;
            Padding3 = 0f;
            Padding4 = 0f;
        }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            Bootstrapper.Configure();
            
            var viewer = SimpleViewer.Create("Phong Shaded Dragon Scene Graph");
            viewer.SetCameraManipulator(TrackballManipulator.Create());

            var root = Group.Create();

            var model = CreateDragonModel();

            var leftTop = MatrixTransform.Create(Matrix4x4.CreateTranslation(-10f, 10f, 0f));
            var rightTop = MatrixTransform.Create(Matrix4x4.CreateTranslation(10f, 10f, 0f));

            var leftBottom = MatrixTransform.Create(Matrix4x4.CreateTranslation(-10f, -10f, 0f));
            var rightBottom = MatrixTransform.Create(Matrix4x4.CreateTranslation(10f, -10f, 0f));
            
            leftTop.AddChild(model);
            rightTop.AddChild(model);
            
            leftBottom.AddChild(model);
            rightBottom.AddChild(model);

            leftTop.PipelineState = CreateHeadlightState(
                Vector3.One, 
                100,
                Vector3.One,
                30);
            
            rightTop.PipelineState = CreateHeadlightState(
                new Vector3(1.0f, 1.0f, 0.0f), 
                50,
                Vector3.One,
                5);
            
            var sceneGroup = Group.Create();
            sceneGroup.AddChild(leftTop);
            sceneGroup.AddChild(rightTop);
            sceneGroup.AddChild(leftBottom);
            sceneGroup.AddChild(rightBottom);

            sceneGroup.PipelineState = CreateSharedHeadlightState();
            
            root.AddChild(sceneGroup);

            viewer.SetSceneData(root);
            viewer.ViewAll();            
            viewer.Run();
        }
        
        public static Stream OpenEmbeddedAssetStream(string name) => Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        
        public static byte[] ReadEmbeddedAssetBytes(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            string[] names = asm.GetManifestResourceNames();
            
            using (Stream stream = OpenEmbeddedAssetStream(name))
            {
                byte[] bytes = new byte[stream.Length];
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    stream.CopyTo(ms);
                    return bytes;
                }
            }
        }
        
        static IGeode CreateDragonModel()
        {
            IGeode result;
            
            using (Stream dragonModelStream = OpenEmbeddedAssetStream(@"Lighting.Assets.Models.chinesedragon.dae"))
            {
                var importer = new Import();
                result = importer.LoadColladaModel(dragonModelStream);
            }
            
            return result;
        }

        private static IPipelineState CreateHeadlightState(
            Vector3 lightColor, 
            float lightPower, 
            Vector3 specularColor,
            float specularPower)
        {
            var pso = PipelineState.Create();

            pso.AddUniform(CreateLight(lightColor, lightPower, specularColor, specularPower, Vector3.One));

            Headlight_Common(ref pso);
            
            return pso;
        }

        private static IPipelineState CreateSharedHeadlightState()
        {
            var pso = PipelineState.Create();
            
            var uniform = Uniform<LightData>.Create(
                "LightData",
                BufferUsage.UniformBuffer | BufferUsage.Dynamic,
                ShaderStages.Vertex, 
                ResourceLayoutElementOptions.DynamicBinding);
            
            var lights = new LightData[]
            {
                // Left Light
                new LightData(
                    new Vector3(1.0f, 1.0f, 1.0f),
                    100,
                    new Vector3(1.0f, 1.0f, 1.0f),
                    5,
                    new Vector3(0.0f, 1.0f, 0.0f),
                1)
                ,
                // Right Light
                new LightData(
                    new Vector3(1.0f, 1.0f, 1.0f),
                    50,
                    new Vector3(1.0f, 1.0f, 1.0f),
                    50,
                    new Vector3(0.0f, 0.0f, 1.0f),
                    1) 
                
            };

            uniform.UniformData = lights;
            pso.AddUniform(uniform);
            
            Headlight_Common(ref pso);
            return pso;
        }

        private static IBindable CreateLight(Vector3 lightColor,
            float lightPower,
            Vector3 specularColor,
            float specularPower,
            Vector3 materialColor,
            int materialOverride = 0)
        {
            var uniform = Uniform<LightData>.Create(
                "LightData",
                BufferUsage.UniformBuffer | BufferUsage.Dynamic,
                ShaderStages.Vertex);
            
            var lights = new LightData[]
            {
                new LightData(
                    lightColor, 
                    lightPower, 
                    specularColor,
                    specularPower,
                    materialColor, 
                    materialOverride)
            };

            uniform.UniformData = lights;

            return uniform;
        }

        private static void Headlight_Common(ref IPipelineState pso)
        {
            var vtxShader =
                new ShaderDescription(ShaderStages.Vertex, ReadEmbeddedAssetBytes(@"Lighting.Assets.Shaders.Phong-vertex.glsl"), "main");
            
            var frgShader =
                new ShaderDescription(ShaderStages.Fragment, ReadEmbeddedAssetBytes(@"Lighting.Assets.Shaders.Phong-fragment.glsl"), "main");
            
            pso.VertexShaderDescription = vtxShader;
            pso.FragmentShaderDescription = frgShader;
        }
    }
}