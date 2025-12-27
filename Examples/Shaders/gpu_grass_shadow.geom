#version 330 core
layout (points) in;
layout (triangle_strip, max_vertices = 24) out;

uniform mat4 lightSpaceMatrix;
uniform vec3 lightDirection; // Light direction for billboard orientation
uniform float grassHeight;
uniform float grassWidth;
uniform float time;
uniform float windStrength;
uniform vec2 windDirection;
uniform float windSpeed;

// Interaction system uniforms (same as main shader)
uniform int numInteractionObjects;
uniform vec3 interactionObjects[32];
uniform float interactionRadius;
uniform float interactionStrength;

// Simple noise function (same as main shader)
float noise(vec2 pos) {
    return fract(sin(dot(pos, vec2(12.9898, 78.233))) * 43758.5453);
}

// Calculate wind displacement at a given height (same as main shader)
vec3 calculateWindOffset(vec3 basePos, float heightRatio) {
    float windInfluence = heightRatio * heightRatio;
    
    float windVariation = sin(time * windSpeed + basePos.x * 2.0 + basePos.z * 1.5) * 0.5 + 0.5;
    windVariation += sin(time * windSpeed * 0.7 + basePos.x * 1.3 + basePos.z * 2.1) * 0.3;
    
    vec3 windOffset = vec3(
        windDirection.x * windStrength * windInfluence * windVariation,
        0.0,
        windDirection.y * windStrength * windInfluence * windVariation
    );
    
    return windOffset;
}

// Calculate interaction displacement (same as main shader)
vec3 calculateInteractionOffset(vec3 basePos, float heightRatio) {
    vec3 totalInteractionOffset = vec3(0.0);
    
    for(int i = 0; i < numInteractionObjects; i++) {
        vec3 objPos = interactionObjects[i];
        
        vec2 grassPos2D = basePos.xz;
        vec2 objPos2D = objPos.xz;
        float distance = length(grassPos2D - objPos2D);
        
        if(distance < interactionRadius && distance > 0.01) {
            vec2 awayDirection = normalize(grassPos2D - objPos2D);
            
            float distanceInfluence = 1.0 - (distance / interactionRadius);
            distanceInfluence = smoothstep(0.0, 1.0, distanceInfluence);
            
            float heightInfluence = heightRatio * heightRatio;
            float totalInfluence = distanceInfluence * heightInfluence * (interactionStrength * 0.1);
            
            totalInteractionOffset.x += awayDirection.x * totalInfluence;
            totalInteractionOffset.z += awayDirection.y * totalInfluence;
        }
    }
    
    return totalInteractionOffset;
}

// Function to check if there's any interaction effect (same as main shader)
bool hasInteractionEffect(vec3 basePos) {
    for(int i = 0; i < numInteractionObjects; i++) {
        vec3 objPos = interactionObjects[i];
        vec2 grassPos2D = basePos.xz;
        vec2 objPos2D = objPos.xz;
        float distance = length(grassPos2D - objPos2D);
        
        if(distance < interactionRadius && distance > 0.01) {
            return true;
        }
    }
    return false;
}

void main()
{
    vec4 basePos = gl_in[0].gl_Position;
    
    // Generate same random height variation as main shader
    vec2 seed = basePos.xz;
    float heightVariation = noise(seed);
    float height = grassHeight * (0.5 + heightVariation * 1.0);
    float width = grassWidth;
    
    // Calculate billboard direction for shadows - face the light direction
    vec3 grassWorldPos = basePos.xyz;
    vec3 toLightDir = normalize(-lightDirection); // Negative because we want to face towards light
    
    // Create right vector perpendicular to light direction (for grass width)
    // We keep the Y component calculation but ensure grass stays vertical
    vec3 rightVector = normalize(cross(vec3(0.0, 1.0, 0.0), toLightDir));
    
    // If light is directly overhead, use a default orientation
    if(length(rightVector) < 0.1) {
        rightVector = vec3(1.0, 0.0, 0.0);
    }
    
    int segments = 5; // Same segment count as main shader
    
    for(int i = 0; i <= segments; i++) {
        float t = float(i) / float(segments);
        float segmentHeight = height * t;
        
        // Calculate wind displacement for this segment
        vec3 windOffset = calculateWindOffset(basePos.xyz, t);
        
        // Calculate interaction displacement for this segment
        vec3 interactionOffset = calculateInteractionOffset(basePos.xyz, t);
        
        // Combine wind and interaction offsets - prioritize interaction over wind
        vec3 totalOffset;
        if(hasInteractionEffect(basePos.xyz)) {
            // If there's interaction, cancel wind and use only interaction
            totalOffset = interactionOffset;
        } else {
            // If no interaction, use wind normally
            totalOffset = windOffset;
        }
        
        vec4 segmentCenter = basePos + vec4(totalOffset.x, segmentHeight, totalOffset.z, 0.0);
        
        // Same width tapering as main shader
        float segmentWidth = width * (1.0 - t * 0.6);
        
        // Left vertex - oriented towards light for better shadow resolution
        vec4 leftPos = segmentCenter + vec4(rightVector * (-segmentWidth * 0.5), 0.0);
        gl_Position = lightSpaceMatrix * leftPos;
        EmitVertex();
        
        // Right vertex - oriented towards light for better shadow resolution
        vec4 rightPos = segmentCenter + vec4(rightVector * (segmentWidth * 0.5), 0.0);
        gl_Position = lightSpaceMatrix * rightPos;
        EmitVertex();
    }
    
    EndPrimitive();
}

