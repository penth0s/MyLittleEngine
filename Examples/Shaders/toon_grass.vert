#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;
layout(location = 3) in vec4 aColor;

out vec2 vTexCoord;
out vec3 vWorldPos;
out vec3 vNormal;
out vec4 vColor;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

uniform sampler2D uWindNoise;
uniform float uWindScroll;
uniform float uWindJitter;
uniform float uTime;

void main()
{
    // World position
    vec4 worldPos = uModel * vec4(aPosition, 1.0);

    // Wind panning based on world XZ
    vec2 windUVBase = worldPos.xz * 0.1;
    vec2 windUVScroll = windUVBase + vec2(uTime * (uWindScroll * 0.3));
    vec2 windUVJitter = windUVBase * 2.0 + vec2(uTime * (uWindJitter * 0.5));

    // Sample wind noise twice and combine
    vec3 wind1 = pow(textureLod(uWindNoise, windUVScroll, 0.0).rgb, vec3(2.5));
    vec3 wind2 = textureLod(uWindNoise, windUVJitter, 0.0).rgb;
    vec3 windOffset = wind1 * wind2 * aColor.rgb;

    // Offset vertex by wind
    worldPos.xyz += windOffset;

    // Output
    vTexCoord = aTexCoord;
    vWorldPos = worldPos.xyz;
    vNormal = mat3(uModel) * aNormal;
    vColor = aColor;

    gl_Position = uProjection * uView * worldPos;
}

