#version 330 core

layout(triangles) in;
layout(triangle_strip, max_vertices = 3) out;

in vec3 Normal[];
in vec3 FragPos[];
in vec2 TexCoords[];

out vec3 gNormal;
out vec3 gFragPos;
out vec2 gTexCoords;
out vec3 vBarycentric;

void main()
{
    for (int i = 0; i < 3; ++i)
    {
        gNormal = normalize(Normal[i]);
        gFragPos = FragPos[i];
        gTexCoords = TexCoords[i];

        vBarycentric = vec3(0.0);
        vBarycentric[i] = 1.0;

        gl_Position = gl_in[i].gl_Position;
        EmitVertex();
    }
    EndPrimitive();
}
