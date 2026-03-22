shader_type canvas_item;

void fragment() {
    vec4 col = texture(TEXTURE, UV);

    float maxC = max(max(col.r, col.g), col.b);
    float minC = min(min(col.r, col.g), col.b);
    float sat = maxC - minC;
    float brightness = (col.r + col.g + col.b) / 3.0;

    // 只去掉"亮灰色"棋盘格（高亮度 + 低饱和度）
    // 怪物本体的暗色/深灰色部分（亮度 < 0.55）完全保留
    if (sat < 0.08 && brightness > 0.55) {
        col.a = 0.0;
    }

    COLOR = col;
}
