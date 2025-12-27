#version 330 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aUV;

out vec2 vNoiseUV;
out vec2 vDistortUV;
out vec3 vViewNormal;
out vec4 vClipPos;
out vec3 vWorldPos;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProj;

void main()
{
    vec4 worldPos = uModel * vec4(aPos, 1.0);
    vWorldPos = worldPos.xyz;
    
    gl_Position = uProj * uView * worldPos;
    vClipPos = gl_Position;
    vNoiseUV = aUV;
    vDistortUV = aUV;
    vViewNormal = mat3(uView) * mat3(uModel) * aNormal;
}