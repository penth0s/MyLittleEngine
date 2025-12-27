#version 330 core

out vec4 FragColor;

uniform sampler2D shadowMap;

void main()
{
    float depth = gl_FragCoord.z;
    
    float near = 0.1;
    float far = 200.0;
    float linearDepth = (2.0 * near) / (far + near - depth * (far - near));
    
    float normalized = pow(linearDepth, 1.0 / 3.2);
    
    FragColor = vec4(vec3(normalized), 1.0);
}

