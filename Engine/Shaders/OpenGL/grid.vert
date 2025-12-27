#version 330 core

layout(location = 0) in vec3 aPosition;

uniform mat4 uMVP;
uniform mat4 uModel;
uniform vec3 uCameraPos;

out vec3 vWorldPos;
out vec3 vCameraPos;

void main()
{
    // Transform position to clip space
    gl_Position = uMVP * vec4(aPosition, 1.0);
    
    // Calculate world position for distance-based fade
    vWorldPos = (uModel * vec4(aPosition, 1.0)).xyz;
    
    // Pass camera position to fragment shader
    vCameraPos = uCameraPos;
}
