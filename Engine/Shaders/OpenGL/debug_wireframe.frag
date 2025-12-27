#version 330 core

in vec4 gColor;
in vec3 bary;

out vec4 color;

float edgeFactor()
{
    vec3 d = fwidth(bary);
    vec3 a3 = smoothstep(vec3(0.0), d * 1.5, bary);
    return min(min(a3.x, a3.y), a3.z);
}

void main()
{
    float edge = edgeFactor();
    float threshold = 0.2;  

    if (edge > threshold)
        discard;

    color = vec4(0,1,0, 1.0);

}

