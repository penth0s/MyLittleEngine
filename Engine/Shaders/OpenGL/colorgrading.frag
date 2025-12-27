#version 330 core

in vec2 TexCoords;
out vec4 FragColor;

uniform sampler2D screenTexture;

// Color Grading Parameters
uniform float exposure;
uniform float contrast;
uniform float saturation;
uniform float temperature;
uniform float tint;

// ACES Tone Mapping
vec3 ACESFilm(vec3 x) {
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

// Saturation
vec3 adjustSaturation(vec3 color, float sat) {
    float luma = dot(color, vec3(0.2126, 0.7152, 0.0722));
    return mix(vec3(luma), color, sat);
}

// Contrast
vec3 adjustContrast(vec3 color, float cont) {
    return (color - 0.5) * cont + 0.5;
}

// White Balance
vec3 adjustWhiteBalance(vec3 color, float temp, float tnt) {
    // Temperature: mavi/sarı
    color *= vec3(1.0 + temp * 0.3, 1.0, 1.0 - temp * 0.3);
    // Tint: yeşil/magenta
    color *= vec3(1.0, 1.0 + tnt * 0.3, 1.0 - tnt * 0.3);
    return color;
}

void main() {
    vec3 color = texture(screenTexture, TexCoords).rgb;
    
    // 1. Exposure
    color *= exposure;
    
    // 2. ACES Tone Mapping (HDR -> LDR)
    color = ACESFilm(color);
    
    // 3. White Balance
    color = adjustWhiteBalance(color, temperature, tint);
    
    // 4. Contrast
    color = adjustContrast(color, contrast);
    
    // 5. Saturation
    color = adjustSaturation(color, saturation);
    
    FragColor = vec4(color, 1.0);
}



