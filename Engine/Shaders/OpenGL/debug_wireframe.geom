#version 330 core

layout(triangles) in;
layout(triangle_strip, max_vertices = 3) out;

in vec4 vColor[];
out vec4 gColor;
out vec3 bary;


void main()
{    
    // 1. vertex
    gl_Position = gl_in[0].gl_Position;
    gColor = vColor[0];
    bary = vec3(1, 0, 0);
    EmitVertex();

    // 2. vertex
    gl_Position = gl_in[1].gl_Position;
    gColor = vColor[1];
    bary = vec3(0, 1, 0);
    EmitVertex();

    // 3. vertex
    gl_Position = gl_in[2].gl_Position;
    gColor = vColor[2];
    bary = vec3(0, 0, 1);
    EmitVertex();

    EndPrimitive();
}

