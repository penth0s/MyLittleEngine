#version 330 core

in vec2 vTexCoord;
in vec3 vWorldPos;
in vec3 vNormal;
in vec4 vColor;

out vec4 FragColor;

uniform sampler2D uMainTex;
uniform vec4 uColor1;
uniform vec4 uColor2;
uniform float uColor1Level;
uniform float uCutoff;

uniform vec3 uLightDir;
uniform vec3 uLightColor;
uniform vec3 uAmbientColor;

uniform bool uUseFog;
uniform vec3 uFogColor;
uniform float uFogStart;
uniform float uFogEnd;
uniform vec3 uViewPos;

void main()
{
    // Base texture
    vec4 texColor = texture(uMainTex, vTexCoord);
    if (texColor.a < uCutoff)
        discard;

    // Vertical gradient between Color1 and Color2
    float gradient = clamp(vTexCoord.y + (uColor1Level - 1.0), 0.0, 1.0);
    vec4 baseColor = mix(uColor2 * texColor, texColor * uColor1, gradient);

    // Lighting (simple Lambert)
    vec3 N = normalize(vNormal);
    vec3 L = normalize(-uLightDir);
    float diff = max(dot(N, L), 0.0);
    vec3 lighting = uAmbientColor + uLightColor * diff;

    vec3 finalColor = baseColor.rgb * lighting;
    
    if (uUseFog)
    {
        float distance = length(vWorldPos - uViewPos);
        float fogFactor = clamp((uFogEnd - distance) / (uFogEnd - uFogStart), 0.0, 1.0);
        finalColor = mix(uFogColor, finalColor, fogFactor);
    }

    FragColor = vec4(finalColor, 1.0);
}


