#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;
layout (location = 3) in ivec4 aBoneIndices;
layout (location = 4) in vec4 aBoneWeights;

uniform mat4 uBoneMatrices[100]; 
uniform mat4 lightSpaceMatrix;
uniform mat4 model;
uniform bool useBones;

void main()
{
    if (!useBones) {
        gl_Position = lightSpaceMatrix * model * vec4(aPos, 1.0);
        return;
    }

  // Calculate weighted bone transformation
    mat4 boneTransform = mat4(0.0);
    
    for(int i = 0; i < 4; i++) {
        if(aBoneWeights[i] > 0.0) {
            boneTransform += uBoneMatrices[aBoneIndices[i]] * aBoneWeights[i];
        }
    }
    
    // If vertex has no bone weights, use identity matrix
    float totalWeight = aBoneWeights.x + aBoneWeights.y + aBoneWeights.z + aBoneWeights.w;
    if (totalWeight == 0.0) {
        boneTransform = mat4(1.0);
    }

    gl_Position = lightSpaceMatrix * model * boneTransform * vec4(aPos, 1.0)   ;
}
