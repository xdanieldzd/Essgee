const int numSamplers = 2;

void main(void)
{
    vec4[numSamplers] output_colors;
    vec4 output_color = vec4(0);

    output_colors[0] = texture(source[0], vertTexCoord);
    output_colors[1] = texture(source[1], vertTexCoord);
    output_color = (output_colors[0] * 0.8) + (output_colors[1] * 0.2);

    // Basic scanlines
    output_color = vec4(clamp(mod(gl_FragCoord.y, 3.0), 0.8, 1.0) * output_color.rgb, 1.0);

    // Gamma correction
    float gamma = 1.2;
    output_color.rgb = pow(output_color.rgb, vec3(1.0 / gamma));

    fragColor = output_color;
}
