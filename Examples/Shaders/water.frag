#version 330 core

in vec2 vNoiseUV;
in vec2 vDistortUV;
in vec3 vViewNormal;
in vec4 vClipPos;
in vec3 vWorldPos;

out vec4 FragColor;

uniform sampler2D uDepthTex;
uniform sampler2D uNormalTex;
uniform sampler2D uNoiseTex;
uniform sampler2D uDistortionTex;
uniform sampler2D uReflectionTex;

uniform vec4 uDepthGradientShallow;
uniform vec4 uDepthGradientDeep;
uniform vec4 uFoamColor;

uniform float uDepthMaxDistance;
uniform float uFoamMaxDistance;
uniform float uFoamMinDistance;
uniform float uSurfaceNoiseCutoff;
uniform float uSurfaceDistortionAmount;
uniform vec2 uSurfaceNoiseScroll;
uniform float uTime;
uniform float uReflectionStrength;
uniform vec2 uScreenSize;
uniform float uPlaneHeight;

vec4 alphaBlend(vec4 top, vec4 bottom)
{
    vec3 color = top.rgb * top.a + bottom.rgb * (1.0 - top.a);
    float alpha = top.a + bottom.a * (1.0 - top.a);
    return vec4(color, alpha);
}

float linearizeDepth(float depth)
{
    float zNear = 0.01;
    float zFar = 1000.0;
    return (2.0 * zNear) / (zFar + zNear - depth * (zFar - zNear));
}

void main()
{
    if (vWorldPos.y < uPlaneHeight - 0.1)
    {
        discard;
    }

    vec2 screenUV = gl_FragCoord.xy / uScreenSize;

    float existingDepth = texture(uDepthTex, screenUV).r;
    float existingDepthLinear = linearizeDepth(existingDepth);

    float fragDepthLinear = linearizeDepth(gl_FragCoord.z);
    float depthDiff = existingDepthLinear - fragDepthLinear;

    float waterDepth01 = clamp(depthDiff / uDepthMaxDistance, 0.0, 1.0);
    vec4 waterColor = mix(uDepthGradientShallow, uDepthGradientDeep, waterDepth01);

    vec3 existingNormal = texture(uNormalTex, screenUV).rgb * 2.0 - 1.0;
    float normalDot = clamp(dot(normalize(existingNormal), normalize(vViewNormal)), 0.0, 1.0);

    float foamDist = mix(uFoamMaxDistance, uFoamMinDistance, normalDot);
    float foamDepth01 = clamp(depthDiff / foamDist, 0.0, 1.0);

    float noiseCutoff = foamDepth01 * uSurfaceNoiseCutoff;

    vec2 distortion = (texture(uDistortionTex, vDistortUV).rg * 2.0 - 1.0) * uSurfaceDistortionAmount;

    vec2 noiseUV = vNoiseUV + uSurfaceNoiseScroll * uTime + distortion;
    float noiseSample = texture(uNoiseTex, noiseUV).r;

    float surfaceNoise = smoothstep(noiseCutoff - 0.01, noiseCutoff + 0.01, noiseSample);

    vec4 foam = uFoamColor;
    foam.a *= surfaceNoise;

    vec2 reflectionUV = screenUV;
    reflectionUV.y = 1.0 - reflectionUV.y;
    
    float timeWave = sin(uTime * 0.5) * 0.00075;
    reflectionUV.x += timeWave;
    reflectionUV += distortion * 0.002;
    reflectionUV = clamp(reflectionUV, 0.001, 0.999);
    
    vec4 reflectionColor = texture(uReflectionTex, reflectionUV);
    vec4 finalColor = mix(waterColor, reflectionColor, uReflectionStrength);
    
    FragColor = alphaBlend(foam, finalColor);
}

