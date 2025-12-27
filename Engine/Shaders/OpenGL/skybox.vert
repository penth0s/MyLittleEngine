#version 330 core

layout(location = 0) in vec3 aPosition;

out vec3 vTexCoords;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    vTexCoords = aPosition;
    
    // Remove translation from view matrix
    mat4 viewRot = mat4(mat3(view));
    vec4 pos = projection * viewRot * vec4(aPosition, 1.0);
    
    // Ensure skybox is always at far plane
    gl_Position = pos.xyww;
}

