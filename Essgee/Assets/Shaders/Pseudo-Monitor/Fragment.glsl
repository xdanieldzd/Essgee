void main(void)
{
    vec4 output_color = texture(source[0], vertTexCoord);

    // Basic scanlines
    output_color = vec4(clamp(mod(gl_FragCoord.y, 3.0), 0.8, 1.0) * output_color.rgb, 1.0);

    // Gamma correction
    float gamma = 1.2;
    output_color.rgb = pow(output_color.rgb, vec3(1.0 / gamma));

    fragColor = output_color;
}
