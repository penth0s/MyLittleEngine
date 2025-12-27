#version 330 core

layout (points) in;
layout (triangle_strip, max_vertices = 24) out;

uniform mat4 mvp;
uniform vec3 cameraPos;
uniform float grassHeight;
uniform float grassWidth;
uniform float time;
uniform float windStrength;
uniform vec2 windDirection;
uniform float windSpeed;

// Interaction system uniforms
uniform int numInteractionObjects;
uniform vec3 interactionObjects[32]; // Support up to 32 interaction objects
uniform float interactionRadius;
uniform float interactionStrength;

out vec3 fragColor;
out float grassHeightNorm;

// Simple noise function
float noise(vec2 pos) {
    return fract(sin(dot(pos, vec2(12.9898, 78.233))) * 43758.5453);
}

// Calculate wind displacement at a given height
vec3 calculateWindOffset(vec3 basePos, float heightRatio) {
    // Wind gets stronger the higher up we go
    float windInfluence = heightRatio * heightRatio; // Quadratic falloff for more natural bending
    
    // Add some variation based on position and time
    float windVariation = sin(time * windSpeed + basePos.x * 2.0 + basePos.z * 1.5) * 0.5 + 0.5;
    windVariation += sin(time * windSpeed * 0.7 + basePos.x * 1.3 + basePos.z * 2.1) * 0.3;
    
    // Calculate wind offset
    vec3 windOffset = vec3(
        windDirection.x * windStrength * windInfluence * windVariation,
        0.0, // No vertical wind displacement
        windDirection.y * windStrength * windInfluence * windVariation
    );
    
    return windOffset;
}

        // Calculate interaction displacement at a given height
    vec3 calculateInteractionOffset(vec3 basePos, float heightRatio) {
    vec3 totalInteractionOffset = vec3(0.0);
    
    // Check all interaction objects
    for(int i = 0; i < numInteractionObjects; i++) {
        vec3 objPos = interactionObjects[i];
        
        // Calculate distance from grass base to interaction object (only XZ plane)
        vec2 grassPos2D = basePos.xz;
        vec2 objPos2D = objPos.xz;
        float distance = length(grassPos2D - objPos2D);
        
        // Only interact if within radius
        if(distance < interactionRadius && distance > 0.01) {
            // Calculate direction away from interaction object
            vec2 awayDirection = normalize(grassPos2D - objPos2D);
            
            // Calculate interaction influence with smoother falloff
            float distanceInfluence = 1.0 - (distance / interactionRadius);
            distanceInfluence = smoothstep(0.0, 1.0, distanceInfluence); // Smoother falloff
            
            // Height influence - grass bends more at the top
            float heightInfluence = heightRatio * heightRatio;
            
            // Scale the interaction strength appropriately
            float totalInfluence = distanceInfluence * heightInfluence * (interactionStrength * 0.1);
            
            // Add to total interaction offset (XZ plane only)
            totalInteractionOffset.x += awayDirection.x * totalInfluence;
            totalInteractionOffset.z += awayDirection.y * totalInfluence; // Note: awayDirection.y maps to world Z
        }
    }
    
    return totalInteractionOffset;
}

// Function to check if there's any interaction effect
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
    
    // Generate random height variation based on position (consistent across frames)
    vec2 seed = basePos.xz;
    float heightVariation = noise(seed);
    
    // Random height between 50% and 150% of base height
    float height = grassHeight * (0.5 + heightVariation * 1.0);
    float width = grassWidth;
    
    // Calculate billboard direction (grass always faces camera)
    vec3 grassWorldPos = basePos.xyz;
    vec3 toCameraDir = normalize(cameraPos - grassWorldPos);
    
    // Create right vector perpendicular to camera direction (for grass width)
    // We keep the Y component as 0 so grass stays vertical
    vec3 rightVector = normalize(cross(vec3(0.0, 1.0, 0.0), toCameraDir));
    
    // Number of segments for smooth bending
    int segments = 5;
    
    for(int i = 0; i <= segments; i++) {
        float t = float(i) / float(segments); // 0 to 1
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
        
        // Apply total displacement
        vec4 segmentCenter = basePos + vec4(totalOffset.x, segmentHeight, totalOffset.z, 0.0);
        
        // Taper the grass blade - wider at base, narrower at tip
        float segmentWidth = width * (1.0 - t * 0.6);
        
        // Left vertex (using billboard right vector)
        vec4 leftPos = segmentCenter + vec4(rightVector * (-segmentWidth * 0.5), 0.0);
        gl_Position = mvp * leftPos;
        
        // Color interpolation from dark green (base) to light green (tip)
        fragColor = mix(vec3(0.1, 0.4, 0.1), vec3(0.4, 0.8, 0.3), t);
        grassHeightNorm = t;
        EmitVertex();
        
        // Right vertex (using billboard right vector)
        vec4 rightPos = segmentCenter + vec4(rightVector * (segmentWidth * 0.5), 0.0);
        gl_Position = mvp * rightPos;
        fragColor = mix(vec3(0.1, 0.4, 0.1), vec3(0.4, 0.8, 0.3), t);
        grassHeightNorm = t;
        EmitVertex();
    }
    
    EndPrimitive();
}



