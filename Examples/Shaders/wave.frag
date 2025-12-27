#version 330 core

in vec2 vUV;
in vec3 vWorldPos;
in vec3 vNormal;

in vec2 vElevation;  // x = wave, y = smallWave
in vec4 vPosition;

out vec4 FragColor;

uniform float uWavesStrength;
uniform vec3 uColorA;
uniform vec3 uColorB;
uniform float uTime;

void main()
{
  
    float elevation = 0.0;
    vec3 color = vec3(0.0);
    float foam = 0.0;

    // Center area logic (high detail zone)
    if(abs(vPosition.x) < 3.5f && abs(vPosition.z) < 3.5f)
    {
        elevation = smoothstep(-uWavesStrength, uWavesStrength, vElevation.x);
        foam += smoothstep(0.5, 0.8, elevation) * 0.1;

        float smallWavesElevation = smoothstep(0.0, uWavesStrength * 0.75, vElevation.y);
        foam += (1.0 - step(0.02, smallWavesElevation));
        foam *= 0.2;
    }
    else
    {
        // Outer area - use world position Y
        elevation = smoothstep(-1.0, uWavesStrength, vWorldPos.y);
    }
   

    // Mix colors based on elevation
    vec3 mixColor = mix(uColorA, uColorB, elevation);
    color += mixColor;
    
    // Add foam
    color += foam;
    
    // Calculate alpha based on depth
    float alpha = smoothstep(uWavesStrength, -0.5, vWorldPos.y);
    alpha = mix(0.8, 0.96, alpha);

    FragColor = vec4(color, alpha);
}
