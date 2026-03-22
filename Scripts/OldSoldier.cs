using Godot;
using System;

[ScriptPath("res://Scripts/OldSoldier.cs")]
public partial class OldSoldier : Node2D
{
    private Area2D _interactArea;
    private Control _levelSelectUI;
    private bool _playerInRange = false;

    public override void _Ready()
    {
        // 渲染老兵NPC视觉 (比奇老兵)
        var sprite = new ColorRect();
        sprite.Size = new Vector2(40, 60);
        sprite.Position = new Vector2(-20, -60);
        sprite.Color = new Color(0.8f, 0.6f, 0.2f); // 金袍银甲
        AddChild(sprite);

        var lbl = new Label();
        lbl.Text = "比奇老兵\n(点我传关)";
        lbl.Position = new Vector2(-40, -95);
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        lbl.AddThemeColorOverride("font_outline_color", Colors.Black);
        lbl.AddThemeConstantOverride("outline_size", 3);
        AddChild(lbl);

        // 交互侦测光环
        _interactArea = new Area2D();
        var shape = new CollisionShape2D();
        var circle = new CircleShape2D();
        circle.Radius = 100f; // 靠近 100 像素内可激活互动
        shape.Shape = circle;
        _interactArea.AddChild(shape);
        AddChild(_interactArea);

        _interactArea.BodyEntered += OnBodyEntered;
        _interactArea.BodyExited += OnBodyExited;

        BuildLevelSelectUI();
    }

    private void BuildLevelSelectUI()
    {
        var canvas = new CanvasLayer();
        canvas.Layer = 100; // 最顶层UI
        
        _levelSelectUI = new Panel();
        _levelSelectUI.Size = new Vector2(500, 430);
        _levelSelectUI.SetAnchorsPreset(Control.LayoutPreset.Center);
        _levelSelectUI.Visible = false;

        // 应用深渊玻璃主题
        UIThemeManager.ApplyPremiumTheme(_levelSelectUI);
        
        var title = new Label();
        title.Text = "=== 远征神殿大厅 ===";
        title.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        title.Position = new Vector2(250 - 80, 20);
        _levelSelectUI.AddChild(title);

        var vbox = new VBoxContainer();
        vbox.Size = new Vector2(400, 300);
        vbox.Position = new Vector2(50, 70);
        vbox.AddThemeConstantOverride("separation", 15);
        _levelSelectUI.AddChild(vbox);

        var levels = new string[] {
            "【第 1 关】 边界村外 (推荐等级 Lv.1)\n掉落：基本生存物资、木剑、布衣",
            "【第 2 关】 毒蛇山谷 (推荐等级 Lv.7)\n掉落：铁剑、轻型盔甲、铜系列首饰",
            "【第 3 关】 古墓深处遗迹 (推荐等级 Lv.15)\n掉落：斩马刀、高级装备碎片",
            "【第 4 关】 尸王殿 (极度危险！！！)\n掉落：★天剑传说★、屠龙圣装"
        };

        for (int i = 0; i < levels.Length; i++) {
            var btn = new Button();
            btn.Text = levels[i];
            btn.CustomMinimumSize = new Vector2(400, 60);
            int levelIdx = i + 1;
            btn.Pressed += () => GoToLevel(levelIdx);
            vbox.AddChild(btn);
        }

        var closeBtn = new Button();
        closeBtn.Text = "返回驻地";
        closeBtn.CustomMinimumSize = new Vector2(120, 40);
        closeBtn.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        closeBtn.Position = new Vector2(250 - 60, 370);
        closeBtn.Pressed += () => { _levelSelectUI.Visible = false; };
        _levelSelectUI.AddChild(closeBtn);

        canvas.AddChild(_levelSelectUI);
        AddChild(canvas);
    }

    private void OnBodyEntered(Node2D body) {
        if (body.IsInGroup("player")) _playerInRange = true;
    }
    
    private void OnBodyExited(Node2D body) {
        if (body.IsInGroup("player")) {
            _playerInRange = false;
            _levelSelectUI.Visible = false;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_playerInRange && @event is InputEventMouseButton m && m.Pressed && m.ButtonIndex == MouseButton.Left)
        {
            var localMouse = GetLocalMousePosition();
            if (localMouse.Length() < 60f) 
            {
                _levelSelectUI.Visible = !_levelSelectUI.Visible;
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void GoToLevel(int levelId)
    {
        GD.Print($"[远征] 传送门启动！前往副本关卡: {levelId}");
        
        // 传递关卡难度参数给单例
        GameManager.Instance.CurrentDungeonLevel = levelId;
        
        // 保存当前在安全区的玩家状态
        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (player != null) player.SaveState();

        // 切换到野外地牢地图
        GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
    }
}
