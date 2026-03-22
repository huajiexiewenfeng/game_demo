shader_type canvas_item;
render_mode blend_add;

void fragment() {
    vec4 col = texture(TEXTURE, UV);
    float distToBlack = distance(col.rgb, vec3(0.0, 0.0, 0.0));
    float distToWhite = distance(col.rgb, vec3(1.0, 1.0, 1.0));
    
    // Smoothly remove both black bounds AND white bounds!
    col.a *= smoothstep(0.05, 0.4, distToBlack) * smoothstep(0.05, 0.4, distToWhite);
    
    COLOR = col;
}
