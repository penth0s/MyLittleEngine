#version 330 core

layout(location = 0) in vec3 aPosition;

out vec3 vDirection;
out float vTime;
out float vRotation; 
out float vRotationSpeed; 

uniform mat4 view;
uniform mat4 projection;

uniform float uRotation;        
uniform float uRotationSpeed;   
uniform float uTime;            
uniform float uCubemapPosition;
uniform bool uEnableRotation;

mat3 rotationY(float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    return mat3(
        c, 0.0, -s,
        0.0, 1.0, 0.0,
        s, 0.0,  c
    );
}

void main()
{
    vec3 pos = aPosition;
    pos.y -= uCubemapPosition;

    if (uEnableRotation)
    {
        float angle = radians(uRotation + uTime * uRotationSpeed);
        pos = rotationY(angle) * pos;
    }

    vDirection = pos;
    vRotationSpeed = uRotationSpeed;
    vRotation = uRotation;
    vTime = uTime;

    mat4 viewRot = mat4(mat3(view)); 
    gl_Position = projection * viewRot * vec4(pos, 1.0);
}

