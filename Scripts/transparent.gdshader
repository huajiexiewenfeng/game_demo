shader_type canvas_item;

void fragment() {
    vec4 col = texture(TEXTURE, UV);
    float distToBlack = distance(col.rgb, vec3(0.0, 0.0, 0.0));
    float distToWhite = distance(col.rgb, vec3(1.0, 1.0, 1.0));
    
    // 极其严苛的剔除：只有真正的极黑和极白才被抠掉！
    // 杜绝角色本体内部带有灰色/深灰色导致全部变成半透明“幽灵”！
    col.a *= smoothstep(0.01, 0.05, distToBlack) * smoothstep(0.01, 0.05, distToWhite);
    
    COLOR = col;
}
