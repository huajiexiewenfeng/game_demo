shader_type canvas_item;

void fragment() {
    vec4 col = texture(TEXTURE, UV);
    // 剔除 AI 绘图生成的纯白背景
    if (col.r > 0.95 && col.g > 0.95 && col.b > 0.95) {
        col.a = 0.0;
    }
    COLOR = col;
}
