using Godot;

public partial class HpOrbUI : Control
{
    private int _currentHp = 100;
    private int _maxHp = 100;
    private float _displayRatio = 1f;
    private float _targetRatio = 1f;
    private float _waveTime = 0f;

    public override void _Ready()
    {
        // 固定尺寸 120x120 的大型圆球
        Size = new Vector2(120, 120);
        CustomMinimumSize = Size;
        ZIndex = 100; // 确保在最前层
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Process(double delta)
    {
        // 平滑插值血量比例
        _displayRatio = Mathf.Lerp(_displayRatio, _targetRatio, (float)delta * 6f);
        _waveTime += (float)delta;
        QueueRedraw(); // 每帧重绘
    }

    public override void _Draw()
    {
        float radius = 55f;
        Vector2 center = new Vector2(60, 60);

        // 1) 黑色外圈描边
        DrawCircle(center, radius + 3, new Color(0, 0, 0, 0.95f));

        // 2) 深暗红色空血底色
        DrawCircle(center, radius, new Color(0.12f, 0.02f, 0.02f, 0.92f));

        // 3) 红色血液填充（从底部填充到对应比例）
        if (_displayRatio > 0.001f)
        {
            // 液面高度：ratio=1 时填满（y=center.y - radius），ratio=0 时空（y=center.y + radius）
            float topY = center.Y + radius - (_displayRatio * radius * 2f);

            // 用多边形逐行扫描绘制圆内区域
            for (float y = topY; y <= center.Y + radius; y += 1f)
            {
                float dy = y - center.Y;
                float halfWidth = Mathf.Sqrt(Mathf.Max(0, radius * radius - dy * dy));

                // 波浪效果
                float wave = Mathf.Sin((_waveTime * 4f) + (y * 0.15f)) * 3f;
                if (y < topY + 5f) // 只在液面表面附近加波浪
                {
                    wave *= (y - topY) / 5f;
                }

                // 绘制这一行的横线
                float x1 = center.X - halfWidth + wave * 0.3f;
                float x2 = center.X + halfWidth + wave * 0.3f;

                // 血液颜色：底部更暗，顶部更亮
                float normalizedY = (y - topY) / Mathf.Max(1f, (center.Y + radius) - topY);
                Color bloodColor = new Color(
                    0.75f + 0.15f * (1f - normalizedY),
                    0.05f + 0.08f * (1f - normalizedY),
                    0.05f,
                    1f
                );

                DrawLine(new Vector2(x1, y), new Vector2(x2, y), bloodColor, 1.5f);
            }
        }

        // 4) 玻璃高光反射（左上角）
        DrawCircle(new Vector2(center.X - 15, center.Y - 18), 12f, new Color(1f, 1f, 1f, 0.18f));
        DrawCircle(new Vector2(center.X - 10, center.Y - 12), 6f, new Color(1f, 1f, 1f, 0.12f));

        // 5) 再画一圈细线描边让球体更立体
        DrawArc(center, radius, 0, Mathf.Tau, 64, new Color(0.3f, 0.05f, 0.05f, 0.7f), 2f);

        // 6) 血量数字
        string hpText = $"{_currentHp}/{_maxHp}";
        var font = ThemeDB.FallbackFont;
        int fontSize = 15;
        Vector2 textSize = font.GetStringSize(hpText, HorizontalAlignment.Center, -1, fontSize);
        Vector2 textPos = new Vector2(center.X - textSize.X / 2f, center.Y + fontSize / 3f);

        // 黑色描边
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (dx != 0 || dy != 0)
                    DrawString(font, textPos + new Vector2(dx, dy), hpText, HorizontalAlignment.Left, -1, fontSize, Colors.Black);

        DrawString(font, textPos, hpText, HorizontalAlignment.Left, -1, fontSize, Colors.White);
    }

    public void UpdateHp(int currentHp, int maxHp)
    {
        _currentHp = currentHp;
        _maxHp = Mathf.Max(1, maxHp);
        _targetRatio = Mathf.Clamp((float)currentHp / _maxHp, 0f, 1f);
    }
}
