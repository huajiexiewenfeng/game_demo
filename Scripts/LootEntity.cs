using Godot;
using System;

[ScriptPath("res://Scripts/LootEntity.cs")]
public partial class LootEntity : Node2D
{
    public string ItemName { get; set; } = "";
    
    private Node2D _visuals;
    private Color _glowColor;
    private Label _nameLabel;
    
    // 状态机：0=爆射喷发中, 1=驻地等待, 2=高潮吸附
    private int _state = 0;
    private float _timer = 0f;
    private float _absorbSpeed = 0f;
    
    public override void _Ready()
    {
        ZIndex = 50; // 凌驾于地面之上
        
        _visuals = new Node2D();
        AddChild(_visuals);

        // 传奇级稀有度颜色分规映射
        if (ItemName.Contains("龙") || ItemName.Contains("神圣"))
            _glowColor = new Color(1.0f, 0.4f, 0.1f);   // 传世橙装（最极品）
        else if (ItemName.Contains("手镯") || ItemName.Contains("戒指") || ItemName.Contains("盔") || ItemName.Contains("甲") || ItemName.Contains("剑"))
            _glowColor = new Color(0.8f, 0.1f, 1.0f);   // 史诗紫装（高级装备）
        else if (ItemName.Contains("金疮药"))
            _glowColor = new Color(0.9f, 0.1f, 0.2f);   // 血红药水
        else
            _glowColor = new Color(1.0f, 0.9f, 0.4f);   // 璀璨金币（杂物）

        // 通过纯代码过程式绘制史诗级钻石光锥实体（告别素材）
        var rect = new ColorRect();
        rect.Color = _glowColor;
        rect.Size = new Vector2(10, 10);
        rect.Position = new Vector2(-5, -5);
        rect.RotationDegrees = 45f;
        _visuals.AddChild(rect);
        
        // 更大一圈的朦胧柔光底座特效
        var rectGlow = new ColorRect();
        rectGlow.Color = new Color(_glowColor.R, _glowColor.G, _glowColor.B, 0.3f);
        rectGlow.Size = new Vector2(24, 24);
        rectGlow.Position = new Vector2(-12, -12);
        rectGlow.RotationDegrees = 45f;
        _visuals.AddChild(rectGlow);

        _nameLabel = new Label();
        _nameLabel.Text = ItemName;
        _nameLabel.AddThemeFontSizeOverride("font_size", 12);
        _nameLabel.AddThemeColorOverride("font_color", _glowColor);
        _nameLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _nameLabel.AddThemeConstantOverride("outline_size", 3);
        _nameLabel.Position = new Vector2(-40, -28);
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _nameLabel.CustomMinimumSize = new Vector2(80, 20);
        _visuals.AddChild(_nameLabel);

        // 核心爆点动画 (The Burst!)
        float angle = (float)(GD.Randf() * Math.PI * 2);
        float dist = 35f + (float)GD.Randf() * 75f; // 长射程喷散半径让掉地不再重叠
        Vector2 targetPos = GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

        var tween = CreateTween();
        // X-Y 平抛位移
        tween.TweenProperty(this, "global_position", targetPos, 0.4f).SetTrans(Tween.TransitionType.Circ).SetEase(Tween.EaseType.Out);
        
        // Z轴视觉弹跳 (修改视觉节点的相对高度)
        var jumpTween = CreateTween();
        jumpTween.TweenProperty(_visuals, "position:y", -45f, 0.2f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        jumpTween.Chain().TweenProperty(_visuals, "position:y", 0f, 0.2f).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
        
        // 动画结束进入驻留观赏期
        tween.TweenCallback(Callable.From(() => { _state = 1; }));
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_state == 1)
        {
            _timer += (float)delta;
            if (_timer >= 1.5f) {
                // 1.5 秒观赏巅峰大爆期结束，触发万剑归宗！
                _state = 2;
                _absorbSpeed = 50f; // 初始加速度
            }
        }
        else if (_state == 2)
        {
            var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
            if (player == null) return;

            Vector2 toPlayer = player.GlobalPosition - this.GlobalPosition;
            float dist = toPlayer.Length();

            if (dist < 15f || _absorbSpeed > 2500f) 
            {
                // 正式吸收到体内！！
                GameManager.Instance.AddToInventory(ItemName);
                QueueFree();
                return;
            }

            // 引力场递增法则：距离越近吸得越快，加速度越来越狂暴
            _absorbSpeed += 1800f * (float)delta; 
            GlobalPosition += toPlayer.Normalized() * _absorbSpeed * (float)delta;
            
            // 在飞行过程中疯狂收缩
            Scale = Scale.Lerp(new Vector2(0.1f, 0.1f), 12f * (float)delta); 
        }
    }
}
