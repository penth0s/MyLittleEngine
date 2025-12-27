#version 330 core

out vec4 FragColor;

in vec2 TexCoord;
in vec3 Normal;
in vec3 WorldPos;

uniform sampler2D uTextureSample;
uniform sampler2D uTextureRamp;
uniform vec3 uLightDir;
uniform vec3 uLightColor;
uniform float uCutoff;
uniform float rampScale = 1.0;

uniform bool uUseFog;
uniform vec3 uFogColor;
uniform float uFogStart;
uniform float uFogEnd;
uniform vec3 uViewPos;

void main()
{
    vec4 texColor = texture(uTextureSample, TexCoord);
    if (texColor.a < uCutoff)
        discard;

    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(uLightDir);

    float diff = dot(norm, lightDir) * 0.5 + 0.5;
    diff = 1.0 - diff;
    vec2 rampUV = vec2(diff, 0.5);
    vec3 rampColor = texture(uTextureRamp, rampUV).rgb ;

    vec3 color = texColor.rgb * (rampColor * rampScale) * uLightColor;
    
    if (uUseFog)
    {
        float distance = length(WorldPos - uViewPos);
        float fogFactor = clamp((uFogEnd - distance) / (uFogEnd - uFogStart), 0.0, 1.0);
        color = mix(uFogColor, color, fogFactor);
    }
    
    FragColor = vec4(color, 1.0);
}

