const int numSamplers = 2;

void main(void)
{
    vec4[numSamplers] output_colors;
    vec4 output_color = vec4(0);

    // Horizontal blur
    vec2 one = 1.0 / textureSize;
    vec4 m2 = texture(source[0], vertTexCoord + vec2((one.s * -2.0), 0.0));
    vec4 m1 = texture(source[0], vertTexCoord + vec2((one.s * -1.0), 0.0));
    vec4 cp = texture(source[0], vertTexCoord + vec2(          0.0,  0.0));
    vec4 p1 = texture(source[0], vertTexCoord + vec2((one.s * +1.0), 0.0));
    vec4 p2 = texture(source[0], vertTexCoord + vec2((one.s * +2.0), 0.0));
    output_colors[0] = ((m2 * 0.025) + (m1 * 0.075) + (cp * 0.8) + (p1 * 0.075) + (p2 * 0.025));

    m2 = texture(source[1], vertTexCoord + vec2((one.s * -2.0), 0.0));
    m1 = texture(source[1], vertTexCoord + vec2((one.s * -1.0), 0.0));
    cp = texture(source[1], vertTexCoord + vec2(          0.0,  0.0));
    p1 = texture(source[1], vertTexCoord + vec2((one.s * +1.0), 0.0));
    p2 = texture(source[1], vertTexCoord + vec2((one.s * +2.0), 0.0));
    output_colors[1] = ((m2 * 0.025) + (m1 * 0.075) + (cp * 0.8) + (p1 * 0.075) + (p2 * 0.025));

    for (int i = 0; i < numSamplers; i++) output_color += output_colors[i] * (1.0 / float(numSamplers));

    fragColor = output_color;
}
