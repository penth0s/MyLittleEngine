#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;
layout (location = 3) in vec4 aColor;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProj;

uniform sampler2D uWindNoiseTexture;
uniform float uWindScroll;
uniform float uWindJitter;
uniform float uTime;

out vec2 TexCoord;
out vec3 Normal;
out vec3 WorldPos;

void main()
{
    vec3 pos = aPos;

    // Wind deformation
    vec4 worldPos = uModel * vec4(pos, 1.0);
    vec2 windUV = worldPos.xz * 0.1;

    vec2 scrollUV = windUV + vec2(uTime * uWindScroll * 0.3);
    vec2 jitterUV = windUV * 2.0 + vec2(uTime * uWindJitter * 0.5);

    vec3 windNoise1 = pow(texture(uWindNoiseTexture, scrollUV).rgb, vec3(2.5));
    vec3 windNoise2 = texture(uWindNoiseTexture, jitterUV).rgb;
    vec3 windOffset = windNoise1 * windNoise2 * aColor.rgb;

    pos += windOffset;

    worldPos = uModel * vec4(pos, 1.0);
    WorldPos = worldPos.xyz;
    Normal = mat3(transpose(inverse(uModel))) * aNormal;
    TexCoord = aTexCoord;

    gl_Position = uProj * uView * worldPos;
}


