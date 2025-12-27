#version 330 core

struct Material {
    int RenderingMode;  
    bool hasAlbedoMap;

    vec4 albedoColor;
    float diffuse;
    float metallic;
    float smoothness;
    float alphaCutoff;
    vec2 tile;
};  

struct LightData {
    int lightType;
    
    vec3 direction;
    vec3 color;
    vec3 position;
    
    float intensity; 
    float range;
    float spotAngle;       
    float innerSpotAngle;
    
    mat4 lightsSpaceMatrix;
};

uniform int shadowMapCount;
uniform int directionalCount;
#define MAX_DIRECTIONAL_LIGHTS 8
uniform LightData lightData[MAX_DIRECTIONAL_LIGHTS];

uniform Material material;
uniform sampler2D albedo; 
uniform vec3 viewPos;
uniform vec3 ambientColor;

uniform sampler2D shadowMaps[MAX_DIRECTIONAL_LIGHTS];

uniform bool useSkybox = true;
uniform samplerCube skyboxCubemap;

uniform bool useWireframe = false;
uniform vec3 lineColor = vec3(0.0, 0.0, 0.0);
uniform float lineThickness = 0.3;

const int MODE_OPAQUE = 0;
const int MODE_CUTOFF = 1;
const int MODE_TRANSPARENT = 2;

const int LIGHT_TYPE_SPOT = 0;
const int LIGHT_TYPE_DIRECTIONAL = 1;
const int LIGHT_TYPE_POINT = 2;

layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec4 NormalOut; 

in vec3 gNormal;
in vec2 gTexCoords;
in vec3 gFragPos;
in vec3 vBarycentric;

// === Helper functions ===
vec3 GetNormal()
{
    return normalize(gNormal);
}

vec3 GetFragPos()
{
    return gFragPos;
}

float EdgeFactor()
{
    vec3 d = fwidth(vBarycentric);
    vec3 a3 = smoothstep(vec3(0.0), d * lineThickness, vBarycentric);
    return min(min(a3.x, a3.y), a3.z);
}

float ShadowCalculation(vec4 fragPosLightSpace, vec3 lightDir, sampler2D shadowMap)
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;

    float currentDepth = projCoords.z;
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0); 
    vec3 normal = GetNormal();
    float bias = max(0.005 * (1.0 - dot(normal, lightDir)), 0.0005);

    for(int x = -1; x <= 1; x++)
    {
        for(int y = -1; y <= 1; y++)
        {
            vec2 offset = vec2(x, y) * texelSize;
            float closestDepth = texture(shadowMap, projCoords.xy + offset).r;
            shadow += currentDepth - bias > closestDepth ? 0 : 1.0;
        }
    }
    shadow /= 9.0; 
    return shadow;
}

vec4 GetAlbedo()
{
    vec4 baseColor = material.albedoColor;
    vec2 tiledUV = gTexCoords * material.tile;

    if (material.hasAlbedoMap)
        return texture(albedo, tiledUV) * baseColor;
    else
        return baseColor;
}

vec3 FresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

vec3 GetLightDiffuse(LightData light, vec3 normal, vec3 fragPos)
{
    if (light.lightType == LIGHT_TYPE_DIRECTIONAL)
    {
        vec3 L = normalize(-light.direction);
        float NdotL = max(dot(normal, L), 0.0);
        return light.color * light.intensity * NdotL;
    }

    if (light.lightType == LIGHT_TYPE_POINT)
    {
        vec3 lightDir = normalize(light.position - fragPos);
        float distance = length(light.position - fragPos);
        float attenuation = clamp(1.0 - (distance / light.range), 0.0, 1.0);
        attenuation *= attenuation;
        float NdotL = max(dot(normal, lightDir), 0.0);
        return light.color * light.intensity * NdotL * attenuation;
    }

    if (light.lightType == LIGHT_TYPE_SPOT)
    {
        vec3 lightDir = normalize(light.position - fragPos);
        float distance = length(light.position - fragPos);
        float attenuation = clamp(1.0 - (distance / light.range), 0.0, 1.0);
        attenuation *= attenuation;

        float NdotL = max(dot(normal, lightDir), 0.0);

        float spotCos = dot(-lightDir, normalize(light.direction));
        float outerAngle = cos(radians(light.spotAngle * 0.5));
        float innerAngle = cos(radians(light.innerSpotAngle * 0.5));
        float spotFactor = clamp((spotCos - outerAngle) / (innerAngle - outerAngle), 0.0, 1.0);

        return light.color * light.intensity * NdotL * attenuation * spotFactor;
    }

    return vec3(0.0);            
}

vec3 GetLightSpecular(LightData light, vec3 normal, vec3 fragPos , vec3 F0)
{
    vec3 N = GetNormal();
    vec3 V = normalize(viewPos - GetFragPos());
    float shininess = mix(4.0, 256.0, material.smoothness);  

    vec3 L = normalize(-light.direction);
    vec3 H = normalize(V + L); 
    float NdotH = max(dot(N, H), 0.0);
    float spec = pow(NdotH, shininess);

    float VdotH = max(dot(V, H), 0.0);
    vec3 F = FresnelSchlick(VdotH, F0); 

    if (light.lightType == LIGHT_TYPE_DIRECTIONAL)
    {       
        return F * spec * light.color * light.intensity;
    }

    if (light.lightType == LIGHT_TYPE_POINT)
    {
        float distance = length(light.position - fragPos);
        float attenuation = clamp(1.0 - distance / light.range, 0.0, 1.0);
        attenuation *= attenuation;  
        return attenuation * F * spec * light.color * light.intensity;   
    }

    return vec3(0.0);  
}

// === MAIN ===
void main()
{
    vec4 albedo = GetAlbedo();
    float alpha = albedo.a;

    if (material.RenderingMode == MODE_OPAQUE) {
        alpha = 1.0;
    }
    else if (material.RenderingMode == MODE_CUTOFF) {
        if (alpha < material.alphaCutoff)
            discard;
    }

    vec3 N = GetNormal();
    vec3 V = normalize(viewPos - GetFragPos());
    float NdotV = max(dot(N, V), 0.0);

    float metallic = material.metallic;
    float roughness = 1.0 - material.smoothness;
    float reflectivity = metallic * material.smoothness;

    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo.rgb, metallic);

    vec3 diffuseColor = albedo.rgb * (1.0 - metallic);
    vec3 diffuse = vec3(0.0);

    for (int i = 0; i < directionalCount; i++)
    {
        diffuse += diffuseColor * GetLightDiffuse(lightData[i], N, GetFragPos());
    }

    float shininess = mix(4.0, 256.0, material.smoothness);    
    vec3 specular = vec3(0.0);

    for (int i = 0; i < directionalCount; i++)
    {      
        specular += GetLightSpecular(lightData[i], N, GetFragPos(), F0);
    }
    
    float shadow = 1;

    if (shadowMapCount > 0)
    {
        shadow = 0.0;
    
        for (int i = 0; i < directionalCount; i++)
        {
            vec4 FragPosLightSpace = lightData[i].lightsSpaceMatrix * vec4(GetFragPos(), 1.0);
            shadow += ShadowCalculation(FragPosLightSpace, normalize(-lightData[i].direction), shadowMaps[i]);
        }
           
        shadow /= float(directionalCount);
        if (shadow > 1)
            shadow = 1;
    }

    vec3 ambient = ambientColor * albedo.rgb;
    vec3 result = (ambient + (diffuse + specular) * shadow);

    if (useSkybox)
    {
        vec3 R = reflect(-V, N);
        vec3 envColor = texture(skyboxCubemap, R).rgb;
        result = mix(result, envColor, reflectivity);
    }
    
    if (useWireframe)
    {
         float edge = EdgeFactor();
         result = mix(vec3(0.0), result, edge);
    }
   
    float gamma = 2.2;
    vec3 correctedColor = pow(result, vec3(1.0 / gamma));

    FragColor = vec4(correctedColor, alpha);
    NormalOut = vec4(normalize(gNormal) * 0.5 + 0.5, 1.0);
}

