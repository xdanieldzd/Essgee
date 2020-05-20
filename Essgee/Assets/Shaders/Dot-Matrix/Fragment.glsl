const int numSamplers = 2;

void main(void)
{
    vec4[numSamplers] output_colors;
    vec4 output_color = vec4(0);
    vec2 grid_step = vec2((outputViewport.z / inputViewport.z), (outputViewport.w / inputViewport.w));
    
    output_colors[0] = texture(source[0], vertTexCoord);
    output_colors[1] = texture(source[1], vertTexCoord);
    for (int i = 0; i < numSamplers; i++) output_color += output_colors[i] * (1.0 / float(numSamplers));

    // Basic grid (horizontal, then vertical)
    if (grid_step.x > 1.0 && grid_step.y > 1.0)
    {
        output_color = vec4(clamp(mod(gl_FragCoord.x - outputViewport.x, grid_step.x), 0.8, 1.0) * output_color.rgb, 1.0);
        output_color = vec4(clamp(mod(gl_FragCoord.y - outputViewport.y, grid_step.y), 0.8, 1.0) * output_color.rgb, 1.0);
    }

    // Gamma correction
    float gamma = 1.2;
    output_color.rgb = pow(output_color.rgb, vec3(1.0 / gamma));

    // Reduce to grayscale & tint
    output_color.rgb = vec3(dot(output_color.rgb, vec3(0.299, 0.587, 0.114))) * vec3(0.878, 0.973, 0.816);

    fragColor = output_color;
}
