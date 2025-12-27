#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;
layout (location = 3) in ivec4 aBoneIndices;
layout (location = 4) in vec4 aBoneWeights;

uniform mat4 transform;  
uniform mat4 view;
uniform mat4 projection;
uniform mat4 uBoneMatrices[100];
uniform bool useBones = false;

out vec3 Normal;
out vec3 FragPos;
out vec2 TexCoords;

void main()
{
    vec4 finalPos;
    vec3 finalNormal;
    
    // Check if skinning is enabled
    if (useBones) {
        // Skinned mesh: Calculate weighted bone transformation
        mat4 boneTransform = mat4(0.0);
        
        for(int i = 0; i < 4; i++) {
            if(aBoneWeights[i] > 0.0) {
                boneTransform += uBoneMatrices[aBoneIndices[i]] * aBoneWeights[i];
            }
        }
        
        // Apply skinning transformation
        finalPos = boneTransform * vec4(aPos, 1.0);
        finalNormal = (boneTransform * vec4(aNormal, 0.0)).xyz;
    } else {
        // Static mesh: Use original position and normal
        finalPos = vec4(aPos, 1.0);
        finalNormal = aNormal;
    }
    
    // Transform to world space
    vec4 worldPos = transform * finalPos;
    FragPos = worldPos.xyz;

    // Transform normal to world space
    mat3 normalMatrix = mat3(transpose(inverse(transform)));
    Normal = normalize(normalMatrix * finalNormal);

    TexCoords = aTexCoords;

    gl_Position = projection * view * worldPos;
}

