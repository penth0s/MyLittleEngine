#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProj;
uniform float uOutlineWidth;
uniform bool uUseOutline;

out vec2 TexCoord;
out vec3 Normal;
out vec3 WorldPos;

void main()
{
    vec3 pos = aPos;
    if (uUseOutline)
        pos += aNormal * uOutlineWidth;

    vec4 worldPos = uModel * vec4(pos, 1.0);
    WorldPos = worldPos.xyz;
    Normal = mat3(transpose(inverse(uModel))) * aNormal;
    TexCoord = aTexCoord;
    gl_Position = uProj * uView * worldPos;
}

