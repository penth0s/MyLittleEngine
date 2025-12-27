#version 330 core

in vec3 vDirection;
in float vTime;
in float vRotation; 
in float vRotationSpeed; 

out vec4 FragColor;

uniform samplerCube uCubemap1;
uniform samplerCube uCubemap2;

uniform float uBlend;           
uniform float uExposure;        
uniform vec3 uTint;             
uniform bool uEnableFog;

uniform vec3 uFogColor;         
uniform float uFogIntensity;    
uniform float uFogHeight;       
uniform float uFogSmoothness;   
uniform float uFogFill;         
uniform float uFogPosition;    

vec3 ApplyFog(vec3 color, vec3 dir)
{
    float h = abs(dir.y + -uFogPosition);
    float fog = pow(clamp(h / max(uFogHeight, 0.0001), 0.0, 1.0), 1.0 - uFogSmoothness);
    fog = mix(fog, 0.0, uFogFill);
    fog = mix(1.0, fog, uFogIntensity);

    return mix(uFogColor, color, fog);
}

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

  vec3 direction = normalize(vDirection);
      
    float angle = radians(vRotation + vTime * vRotationSpeed);
              direction = rotationY(angle) * direction;

    vec3 col1 = texture(uCubemap1, normalize(direction)).rgb;
    vec3 col2 = texture(uCubemap2, normalize(direction)).rgb;

    vec3 blended = mix(col1, col2, uBlend);
    blended *= uTint * uExposure;

    if (uEnableFog)
        blended = ApplyFog(blended, normalize(vDirection));

    FragColor = vec4(blended, 1.0);
}





