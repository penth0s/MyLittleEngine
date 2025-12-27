#version 330 core

in vec3 fragColor;
in float grassHeightNorm;

out vec4 FragColor;

void main()
{
    // Base grass color with height variation
    vec3 color = fragColor;
    
    // Add subtle lighting effect - darker in middle, lighter on edges
    float edgeFactor = abs(gl_FragCoord.x - floor(gl_FragCoord.x) - 0.5) * 2.0;
    color = mix(color, color * 1.2, edgeFactor * 0.3);
    
    // Add slight transparency at the tips for more natural look
    float alpha = mix(1.0, 0.9, grassHeightNorm * 0.3);
    
    FragColor = vec4(color, alpha);
}

