#version 330 core

in vec3 vWorldPos;
in vec3 vCameraPos;

out vec4 FragColor;

// Grid appearance settings
const vec3 gridColor = vec3(0.3, 0.3, 0.3);
const float fadeStart = 50.0;   // Distance where fade begins
const float fadeEnd = 350.0;    // Distance where grid fully fades out
const float baseAlpha = 0.5;    // Base transparency of grid lines

void main()
{
    // Calculate distance from camera to fragment
    float distance = length(vWorldPos - vCameraPos);
    
    // Calculate fade factor based on distance
    float fadeFactor = 1.0 - smoothstep(fadeStart, fadeEnd, distance);
    
    // Calculate final alpha with base transparency
    float alpha = baseAlpha * fadeFactor;
    
    // Output final color with fade
    FragColor = vec4(gridColor, alpha);
}
