#version 330 core
out vec4 FragColor;

in vec2 TexCoord;

uniform sampler2D screenTexture;
uniform float intensity;
uniform float smoothness;
uniform float roundness;
uniform vec2 screenResolution;

void main()
{
    vec4 color = texture(screenTexture, TexCoord);
    
    vec2 uv = TexCoord - 0.5;
    
    float aspectRatio = screenResolution.x / screenResolution.y;
    uv.x *= mix(1.0, aspectRatio, roundness);
    
    float dist = length(uv);
    
    float vignette = smoothstep(0.8, 0.8 - smoothness, dist * intensity);
    
    FragColor = vec4(color.rgb * vignette, color.a);
}

