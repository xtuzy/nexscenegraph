#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

struct FragmentInput
{
    vec4 Position;
    vec3 TexCoord;
    vec4 Color;
};

vec4 CalculateColor(FragmentInput input_, texture3D volumeTexture, sampler volumeSampler) 
{
    vec4 output_ = texture(sampler3D(volumeTexture, volumeSampler), input_.TexCoord);
    if (input_.TexCoord.x > 1.0 || input_.TexCoord.y > 1.0 || input_.TexCoord.z > 1.0
    || input_.TexCoord.x < 0.0 || input_.TexCoord.y < 0.0 || input_.TexCoord.z < 0.0)
    {
        output_ = vec4(1.0, 1.0, 1.0, 0.0);
    }
    return output_;
}

layout(set = 1, binding = 1) uniform texture3D SurfaceTexture;
layout(set = 1, binding = 2) uniform sampler SurfaceSampler;

layout(location = 0) in vec4 fsin_0;
layout(location = 1) in vec3 fsin_1;
layout(location = 0) out vec4 OutputColor;

void main()
{
    FragmentInput input_;
    input_.Position = gl_FragCoord;
    input_.Color = fsin_0;
    input_.TexCoord = fsin_1;
    
    vec4 output_ = CalculateColor(input_, SurfaceTexture, SurfaceSampler);
    
    OutputColor = output_;
}