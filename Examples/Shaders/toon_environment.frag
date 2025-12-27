#version 330 core

layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec4 NormalOut;

in vec2 TexCoord;
in vec3 Normal;
in vec3 WorldPos;

uniform sampler2D uTextureSample;
uniform sampler2D uTextureRamp;
uniform vec3 uLightDir;
uniform vec3 uLightColor;
uniform vec3 uViewPos;
uniform vec3 uOutlineColor;
uniform bool uUseOutline;
uniform float rampScale;

uniform bool uUseFog;
uniform vec3 uFogColor;
uniform float uFogStart;
uniform float uFogEnd;

void main()
{
    if (uUseOutline)
    {
        FragColor = vec4(uOutlineColor, 1.0);
        NormalOut = vec4(normalize(Normal) * 0.5 + 0.5, 1.0);
        return;
    }

    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(uLightDir);
    float diff = dot(norm, lightDir) * 0.5 + 0.5;
    diff = 1.0 - diff;
    vec2 rampUV = vec2(diff, 0.5);
    vec3 rampColor = texture(uTextureRamp, rampUV).rgb * rampScale;
    vec3 baseColor = texture(uTextureSample, TexCoord).rgb;
    vec3 finalColor = baseColor * rampColor * uLightColor;
    
    if (uUseFog)
    {
        float distance = length(WorldPos - uViewPos);
        float fogFactor = clamp((uFogEnd - distance) / (uFogEnd - uFogStart), 0.0, 1.0);
        finalColor = mix(uFogColor, finalColor, fogFactor);
    }
    
    FragColor = vec4(finalColor, 1.0);
    NormalOut = vec4(norm * 0.5 + 0.5, 1.0);
}

