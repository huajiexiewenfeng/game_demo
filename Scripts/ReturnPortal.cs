using Godot;
using System;

[ScriptPath("res://Scripts/ReturnPortal.cs")]
public partial class ReturnPortal : Node2D
{
    private Area2D _interactArea;
    private bool _playerInRange = false;

    public override void _Ready()
    {
        ZIndex = 40;

        var outerGlow = new ColorRect();
        outerGlow.Size = new Vector2(100, 100);
        outerGlow.Position = new Vector2(-50, -50);
        outerGlow.Color = new Color(0.1f, 0.4f, 1f, 0.3f); // 幽蓝传送门
        AddChild(outerGlow);

        var innerCore = new ColorRect();
        innerCore.Size = new Vector2(40, 80);
        innerCore.Position = new Vector2(-20, -40);
        innerCore.Color = new Color(0.8f, 1.0f, 1.0f, 0.9f); // 发光核心
        AddChild(innerCore);

        var lbl = new Label();
        lbl.Text = "回城传送阵\n(点击返回)";
        lbl.Position = new Vector2(-40, -80);
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        lbl.AddThemeColorOverride("font_color", new Color(0.5f, 1f, 1f));
        lbl.AddThemeColorOverride("font_outline_color", Colors.Black);
        lbl.AddThemeConstantOverride("outline_size", 4);
        AddChild(lbl);

        _interactArea = new Area2D();
        var shape = new CollisionShape2D();
        var circle = new CircleShape2D();
        circle.Radius = 60f;
        shape.Shape = circle;
        _interactArea.AddChild(shape);
        AddChild(_interactArea);

        _interactArea.BodyEntered += OnBodyEntered;
        _interactArea.BodyExited += OnBodyExited;

        var animTween = CreateTween().SetLoops();
        animTween.TweenProperty(outerGlow, "rotation", Mathf.Pi * 2, 4f).AsRelative();
        animTween.Parallel().TweenProperty(innerCore, "scale", new Vector2(1.2f, 1.2f), 1f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        animTween.Chain().TweenProperty(innerCore, "scale", Vector2.One, 1f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        
        // 凭空刷出的视觉特效
        Scale = Vector2.Zero;
        CreateTween().TweenProperty(this, "scale", Vector2.One, 1.5f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
    }
    
    private void OnBodyEntered(Node2D body) {
        if (body.IsInGroup("player")) _playerInRange = true;
    }
    private void OnBodyExited(Node2D body) {
        if (body.IsInGroup("player")) _playerInRange = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (_playerInRange && @event is InputEventMouseButton m && m.Pressed && m.ButtonIndex == MouseButton.Left)
        {
            var localMouse = GetLocalMousePosition();
            if (localMouse.Length() < 60f) 
            {
                GetViewport().SetInputAsHandled();
                ExtractToSafeZone();
            }
        }
    }

    private void ExtractToSafeZone()
    {
        GD.Print("[传送] 正在满载而归返回安全区 (比奇城)...");
        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (player != null) player.SaveState(); // 带着这趟副本爆出的所有奖励保存
        GetTree().ChangeSceneToFile("res://Scenes/SafeZone.tscn");
    }
}
