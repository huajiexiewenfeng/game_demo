using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

[ScriptPath("res://Scripts/Player.cs")]
public partial class Player : CharacterBody2D
{

	[Export(PropertyHint.None, "")]
	public float Speed = 200f;

	[Export(PropertyHint.None, "")]
	public int Hp = 100;

	[Export(PropertyHint.None, "")]
	public int MaxHp = 100;

	[Export(PropertyHint.None, "")]
	public int Level = 1;

	public int Exp = 0;

	[Export(PropertyHint.None, "")]
	public int BaseAttackPower = 10;

	[Export(PropertyHint.None, "")]
	public float AttackRange = 90f;

	[Export(PropertyHint.None, "")]
	public float AttackCooldown = 1f;

	private Vector2 targetPosition;

	private bool isMoving = false;

	private Enemy targetEnemy = null;

	private float lastAttackTime = 0f;

	private bool _targetSetThisFrame = false;

	private Label _levelLbl;

	private Label _hpLbl;

	private Label _atkLbl;

	private Label _weaponLbl;

	private Label _expLbl;

	private InventoryUI _inventoryUI;

	private SkillUI _skillUI;

	private CharacterAnimator _animator;

	private Camera2D _camera;

	public int ExpToNextLevel => Level * 100;

	public int FinalAttackPower { get; private set; }
    public int FinalDefense { get; private set; }
    public int FinalMaxHp { get; private set; }
    
    public int CurrentAttackPower => FinalAttackPower;

	public ItemData EquippedWeapon { get; private set; } = null;
    
    private EquipmentUI _equipmentUI;
    private HpOrbUI     _hpOrb;

	public override void _Ready()
	{
		targetPosition = base.GlobalPosition;
        CleanPlayerSheetBorders(); // 直接物理修改并剔除图集边框
		_levelLbl = GetNodeOrNull<Label>("HUD/StatsPanel/VBox/LevelLbl");
		_hpLbl = GetNodeOrNull<Label>("HUD/StatsPanel/VBox/HpLbl");
		_atkLbl = GetNodeOrNull<Label>("HUD/StatsPanel/VBox/AtkLbl");
		_weaponLbl = GetNodeOrNull<Label>("HUD/StatsPanel/VBox/WeaponLbl");
		_expLbl = GetNodeOrNull<Label>("HUD/StatsPanel/VBox/ExpLbl");
		_inventoryUI = GetNodeOrNull<InventoryUI>("HUD/InventoryUI");
		Button nodeOrNull = GetNodeOrNull<Button>("HUD/StatsPanel/VBox/BagBtn");
		if (nodeOrNull != null && _inventoryUI != null)
		{
			nodeOrNull.Pressed += _inventoryUI.Toggle;
		}
		_skillUI = GetNodeOrNull<SkillUI>("HUD/SkillUI");
		_animator = GetNodeOrNull<CharacterAnimator>("Animator");
        
        _equipmentUI = new EquipmentUI();
        _equipmentUI.Name = "EquipmentUI";
        GetNodeOrNull<CanvasLayer>("HUD")?.AddChild(_equipmentUI);
        
		Sprite2D spr = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (spr != null) spr.Scale *= 2f;
		_animator?.SyncOrigScale();

		_camera = GetNodeOrNull<Camera2D>("Camera2D");
		AddToGroup("player");
		
        // 应用全新高级UI并重构布局
        var hud = GetNodeOrNull<CanvasLayer>("HUD");
        if (hud != null) {
            UIThemeManager.ApplyPremiumTheme(hud);
            var vpX = GetViewportRect().Size.X;
            var vpY = GetViewportRect().Size.Y;
            
            var invUI = hud.GetNodeOrNull<Control>("InventoryUI");
            if (invUI != null) {
                invUI.Position = new Vector2(vpX - 280, 20);
                var invPanel = invUI.GetNodeOrNull<Control>("Panel");
                if (invPanel != null) invPanel.Position = Vector2.Zero;
            }
            
            var skillUI = hud.GetNodeOrNull<Control>("SkillUI");
            if (skillUI != null) {
                skillUI.Position = new Vector2(vpX - 980, 20); // 往左移避开背包界面，为 650px 宽度的超大技能面板腾出安全空间
                var skillPanel = skillUI.GetNodeOrNull<Control>("Panel");
                if (skillPanel != null) skillPanel.Position = Vector2.Zero;
            }
            
        var hotbar = hud.GetNodeOrNull<Control>("SkillHotbar");
        if (hotbar != null) {
            hotbar.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
            hotbar.Position = new Vector2(vpX/2 - hotbar.Size.X/2, vpY - 100);
        }

        // --- 左下角经典红色血球 ---
        _hpOrb = new HpOrbUI();
        _hpOrb.Name = "HpOrb";
        _hpOrb.Position = new Vector2(15, vpY - 135); // 左下角，与技能栏不重叠
        _hpOrb.ZIndex = 200; // 确保最顶层
        hud.AddChild(_hpOrb);
        
        // 强力回城热键 (全局悬浮可见，防走失)
        var homeBtn = new Button();
        homeBtn.Text = "【 回城 】";
        homeBtn.CustomMinimumSize = new Vector2(100, 35);
        homeBtn.Size = new Vector2(100, 35);
        homeBtn.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        // 放置在屏幕左上方完全安全的绝对坐标，无论窗口怎么缩放绝不会飘出屏幕外
        homeBtn.Position = new Vector2(280, 20); 
        homeBtn.AddThemeColorOverride("font_color", new Color(0.9f, 0.5f, 1f));
        homeBtn.Pressed += () => {
            SaveState();
            GetTree().ChangeSceneToFile("res://Scenes/SafeZone.tscn");
        };
        hud.AddChild(homeBtn);
    }
    
    // 初始化同步跨场景加载
    LoadStateFromGameManager();
    RecalculateFinalStats();
}

public void LoadStateFromGameManager()
{
    Level = GameManager.Instance.PlayerLevel;
    Exp = GameManager.Instance.PlayerExp;
    Hp = GameManager.Instance.PlayerHp;
    MaxHp = GameManager.Instance.PlayerMaxHp;
    BaseAttackPower = GameManager.Instance.PlayerBaseAtk;
    
    // 安全兜底：永远不以 0 血量出生
    if (Hp <= 0) {
        Hp = MaxHp > 0 ? MaxHp : 100;
        GameManager.Instance.PlayerHp = Hp;
    }
    _isDead = false; // 新场景重置死亡状态
}

public void RecalculateFinalStats()
{
    FinalMaxHp = MaxHp;
    FinalAttackPower = BaseAttackPower;
    FinalDefense = 0;
    EquippedWeapon = null;
    
    string[] gearNames = {
        GameManager.Instance.EquippedWeapon, GameManager.Instance.EquippedArmor,
        GameManager.Instance.EquippedHelmet, GameManager.Instance.EquippedNecklace,
        GameManager.Instance.EquippedBracelet, GameManager.Instance.EquippedRing
    };

    foreach (var itemName in gearNames) {
        if (!string.IsNullOrEmpty(itemName) && GameManager.Instance.ItemDatabase.TryGetValue(itemName, out ItemData gear)) {
            FinalMaxHp += gear.HpBonus;
            FinalDefense += gear.DefenseBonus;
            FinalAttackPower += gear.AttackBonus;
            if (gear.Type == ItemData.ItemType.Weapon) EquippedWeapon = gear;
        }
    }
    
    Hp = Mathf.Min(Hp, FinalMaxHp);
    UpdateUI();
}

public void SaveState()
{
    GameManager.Instance.PlayerLevel = Level;
    GameManager.Instance.PlayerExp = Exp;
    GameManager.Instance.PlayerHp = Hp;
    GameManager.Instance.PlayerMaxHp = MaxHp; // 保存裸装血量上限
    GameManager.Instance.PlayerBaseAtk = BaseAttackPower;
}

	public void RecalculateStats()
	{
		RecalculateFinalStats();
	}

	private void UpdateUI()
	{
		if (_levelLbl != null)
		{
			_levelLbl.Text = $"等级: {Level}";
		}
		if (_hpLbl != null)
		{
			_hpLbl.Text = $"生命值: {Hp}/{FinalMaxHp}";
		}
		if (_atkLbl != null)
		{
			_atkLbl.Text = $"总攻击力: {CurrentAttackPower}";
		}
		if (_weaponLbl != null)
		{
			_weaponLbl.Text = "当前装备: " + ((EquippedWeapon != null) ? EquippedWeapon.ItemName : "无");
		}
		if (_expLbl != null)
		{
			_expLbl.Text = $"Exp: {Exp}/{ExpToNextLevel}";
		}
        
        // 刷新左下角红色血球
        _hpOrb?.UpdateHp(Hp, FinalMaxHp);
	}

    private bool _isDead = false;

    public void TakeDamage(int dmg)
    {
        if (_isDead) return;

        int actual = Mathf.Max(1, dmg - FinalDefense); // 应用防御抵伤
        Hp -= actual;
        
        ShakeCamera();
        
        // --- 死亡判定 ---
        if (Hp <= 0)
        {
            Hp = 0;
            _isDead = true;
            SaveState();
            UpdateUI();
            ExecutePermadeathSequence();
            return;
        }

        SaveState(); 
        UpdateUI();
    }

    private async void ExecutePermadeathSequence()
    {
        InputPickable = false;
        SetPhysicsProcess(false);
        SetProcessUnhandledInput(false);

        // 角色变红 + 倒地动画
        Modulate = new Color(0.8f, 0.1f, 0.1f, 0.7f);
        var deathSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (deathSprite != null) {
            var tw = CreateTween();
            tw.TweenProperty(deathSprite, "rotation", Mathf.Pi / 2f, 0.5f);
        }

        // 全屏显示死亡提示
        var hud = GetNodeOrNull<CanvasLayer>("HUD");
        if (hud != null) {
            var deathObj = new Label();
            deathObj.Text = "【 胜败乃兵家常事，5 秒后回主城复活... 】";
            deathObj.SetAnchorsPreset(Control.LayoutPreset.Center);
            deathObj.Position = new Vector2(1280f/2 - 300, 720f/2 - 50);
            deathObj.AddThemeFontSizeOverride("font_size", 26);
            deathObj.AddThemeColorOverride("font_color", Colors.Red);
            deathObj.AddThemeColorOverride("font_outline_color", Colors.Black);
            deathObj.AddThemeConstantOverride("outline_size", 5);
            hud.AddChild(deathObj);
        }

        // 5 秒等待
        await ToSignal(GetTree().CreateTimer(5.0f), SceneTreeTimer.SignalName.Timeout);

        // 关键：先满血，再存档，再切换场景
        Hp = FinalMaxHp;
        MaxHp = GameManager.Instance.PlayerMaxHp;
        GameManager.Instance.PlayerHp = Hp; // 直接写入满血
        GameManager.Instance.CurrentDungeonLevel = 1;
        SaveState();
        
        GetTree().ChangeSceneToFile("res://Scenes/SafeZone.tscn");
    }

    // 从物理层面强制擦除贴图文件中残留的排版网格线和抗锯齿脏边
    private void CleanPlayerSheetBorders() {
        string imgPath = ProjectSettings.GlobalizePath("res://Sprites/player_sheet.png");
        var img = new Image();
        if (img.Load(imgPath) == Error.Ok) {
            int w = img.GetWidth(); int h = img.GetHeight();
            int fw = w / 2; int fh = h / 2;
            for (int r=0; r<2; r++) {
                for (int c=0; c<2; c++) {
                    int sx = c * fw; int sy = r * fh;
                    // 强制清空每个局部贴图的最外围 2 像素（彻底消灭漏边）
                    for (int x=sx; x<sx+fw; x++) {
                        img.SetPixel(x, sy, new Color(0,0,0,0));
                        img.SetPixel(x, sy+1, new Color(0,0,0,0));
                        img.SetPixel(x, sy+fh-1, new Color(0,0,0,0));
                        img.SetPixel(x, sy+fh-2, new Color(0,0,0,0));
                    }
                    for (int y=sy; y<sy+fh; y++) {
                        img.SetPixel(sx, y, new Color(0,0,0,0));
                        img.SetPixel(sx+1, y, new Color(0,0,0,0));
                        img.SetPixel(sx+fw-1, y, new Color(0,0,0,0));
                        img.SetPixel(sx+fw-2, y, new Color(0,0,0,0));
                    }
                }
            }
            // 覆写原图，一劳永逸
            img.SavePng(imgPath);
            // 热重载到当前精灵上保证瞬间生效
            var spr = GetNodeOrNull<Sprite2D>("Sprite2D");
            if (spr != null) spr.Texture = ImageTexture.CreateFromImage(img);
        }
    }

	public void EquipWeapon(ItemData weapon)
	{
		// 历史兼容：转发给统一穿戴引擎，不要直接改写单槽了
		GameManager.Instance.EquipItem(weapon);
	}

	public void SetTargetEnemy(Enemy enemy)
	{
		targetEnemy = enemy;
		_targetSetThisFrame = true;
		WalkTo(enemy.GlobalPosition);
		GD.Print("[追击] 锁定目标: " + enemy.MonsterName);
	}

	public void ClearTarget()
	{
		targetEnemy = null;
		isMoving = false;
		base.Velocity = Vector2.Zero;
	}

	private void WalkTo(Vector2 point)
	{
		targetPosition = point;
		isMoving = true;
	}

	public override void _PhysicsProcess(double delta)
	{
		lastAttackTime += (float)delta;
		_targetSetThisFrame = false;
		if (GodotObject.IsInstanceValid(targetEnemy))
		{
			targetPosition = targetEnemy.GlobalPosition;
			float num = base.GlobalPosition.DistanceTo(targetPosition);
			if (num <= AttackRange)
			{
				isMoving = false;
				base.Velocity = Vector2.Zero;
				if (lastAttackTime >= AttackCooldown)
				{
					Attack(targetEnemy);
					lastAttackTime = 0f;
				}
			}
			else
			{
				isMoving = true;
				Vector2 vector = base.GlobalPosition.DirectionTo(targetPosition);
				base.Velocity = vector * Speed;
			}
		}
		else if (isMoving)
		{
			Vector2 vector2 = base.GlobalPosition.DirectionTo(targetPosition);
			base.Velocity = vector2 * Speed;
			if (base.GlobalPosition.DistanceTo(targetPosition) < 5f)
			{
				isMoving = false;
				base.Velocity = Vector2.Zero;
			}
		}
		else
		{
			base.Velocity = Vector2.Zero;
		}
		MoveAndSlide();
		_animator?.Update((float)delta, isMoving, lastAttackTime < 0.15f, base.Velocity);
	}

	private void Attack(Enemy enemy)
	{
        if (GetTree().CurrentScene.Name == "SafeZone") return; // 安全区绝对禁武
		GD.Print($"[玩家] 挥动{((EquippedWeapon != null) ? EquippedWeapon.ItemName : "拳头")}砍向 {enemy.MonsterName}，造成 {CurrentAttackPower} 伤害！");
		enemy.TakeDamage(CurrentAttackPower);
		_animator?.PlayAttack();
		ShakeCamera();
        SpawnSwordAura(enemy); // 触发金色剑气特效！
	}

    // 二进制无报错静默加载器
    private Texture2D LoadTextureRobust(string path) {
        string globalPath = ProjectSettings.GlobalizePath(path);
        if (!System.IO.File.Exists(globalPath)) return null;

        try {
            byte[] bytes = System.IO.File.ReadAllBytes(globalPath);
            if (bytes.Length < 12) return null;

            var img = new Image();
            Error err = Error.Failed;
            if (bytes[0] == 0x52 && bytes[1] == 0x49) err = img.LoadWebpFromBuffer(bytes); 
            else if (bytes[0] == 0x89 && bytes[1] == 0x50) err = img.LoadPngFromBuffer(bytes); 
            else if (bytes[0] == 0xFF && bytes[1] == 0xD8) err = img.LoadJpgFromBuffer(bytes);

            if (err == Error.Ok && img.GetWidth() > 0) return ImageTexture.CreateFromImage(img);
        } catch { }
        return null;
    }

    private void SpawnSwordAura(Enemy target)
    {
        // 动态生成半透明月牙形剑气轨迹 (Procedural Trail!)
        var slash = new Line2D();
        slash.Width = 30f; // 更加霸气的剑体厚度
        
        var grad = new Gradient();
        grad.AddPoint(0f, new Color(1f, 1f, 1f, 0f));
        grad.AddPoint(0.5f, new Color(1f, 0.9f, 0.4f, 1f));
        grad.AddPoint(1f, new Color(1f, 0.2f, 0f, 0f)); // 尾端消散成赤红
        slash.Gradient = grad;
        
        // 构造极具张力的半月弧顶顶点
        Vector2 dir = this.GlobalPosition.DirectionTo(target.GlobalPosition);
        float angleStr = dir.Angle();
        var points = new System.Collections.Generic.List<Vector2>();
        for(int i=-8; i<=8; i++){
            float a = angleStr + (i * 0.12f);
            float dist = 60f - Mathf.Abs(i * 2.5f); // 尖端变细收拢
            points.Add(this.GlobalPosition + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * dist);
        }
        slash.Points = points.ToArray();
        slash.JointMode = Line2D.LineJointMode.Round;
        slash.EndCapMode = Line2D.LineCapMode.Round;
        slash.BeginCapMode = Line2D.LineCapMode.Round;
        
        var mat = new CanvasItemMaterial();
        mat.BlendMode = CanvasItemMaterial.BlendModeEnum.Add; // 纯净加法发光
        slash.Material = mat;
        slash.ZIndex = 120;
        
        GetParent().AddChild(slash);
        
        var tween = slash.CreateTween();
        Vector2 flyDir = this.GlobalPosition + dir * 80f; // 前冲刺打击感
        tween.TweenProperty(slash, "global_position", flyDir, 0.2f).SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(slash, "scale", new Vector2(1.5f, 1.5f), 0.2f);
        tween.Parallel().TweenProperty(slash, "modulate:a", 0f, 0.2f);
        tween.TweenCallback(Callable.From(() => { if (GodotObject.IsInstanceValid(slash)) slash.QueueFree(); }));
    }

	private void ShakeCamera()
	{
		if (_camera == null) return;
		var tween = CreateTween();
		tween.TweenProperty(_camera, "offset", new Vector2(GD.Randf() * 12 - 6, GD.Randf() * 12 - 6), 0.05f);
		tween.TweenProperty(_camera, "offset", new Vector2(GD.Randf() * -12 + 6, GD.Randf() * -12 + 6), 0.05f);
		tween.TweenProperty(_camera, "offset", Vector2.Zero, 0.05f);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false } inputEventKey)
		{
			// UI 唤出热键
			if (inputEventKey.Keycode == Key.B) { _inventoryUI?.Toggle(); return; }
			if (inputEventKey.Keycode == Key.K) { _skillUI?.Toggle(); return; }
			if (inputEventKey.Keycode == Key.C) { _equipmentUI?.Toggle(); return; }

			// 动态快捷键读取 (键位 1-5)
			int keyIndex = (int)(inputEventKey.Keycode - Key.Key1);
			if (keyIndex >= 0 && keyIndex <= 4)
			{
				SkillManager instance = SkillManager.Instance;
				if (instance != null && instance.HotbarMapping.TryGetValue(keyIndex, out SkillData skillData))
				{
					if (skillData.Effect == SkillData.EffectType.Heal)
					{
						instance.TriggerSkill(skillData, this, null);
						GD.Print($"[快捷键] 触发槽位[{keyIndex + 1}]：{skillData.SkillName}");
					}
					else if (GodotObject.IsInstanceValid(targetEnemy))
					{
						if (GetTree().CurrentScene.Name == "SafeZone") {
							GD.Print("[安全区警告] 比奇城内禁止挥动兵器与施展法术！");
							return;
						}
						instance.TriggerSkill(skillData, this, targetEnemy);
						GD.Print($"[快捷键] 施放[{keyIndex + 1}] {skillData.SkillName} 锁定目标: {targetEnemy.MonsterName}");
					}
					else
					{
						GD.Print("[瞄准警告] 这项强力技能需要先用鼠标左键锁定一个具体目标！");
					}
				}
                else 
                {
                    GD.Print($"[快捷键] 槽位 {keyIndex + 1} 当前尚未绑定任何法术。");
                }
				return;
			}
		}

		if (@event is InputEventMouseButton { Pressed: true } mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && !_targetSetThisFrame)
		{
			ClearTarget();
			WalkTo(GetGlobalMousePosition());
		}
	}

	public void GainExp(int exp)
	{
		Exp += exp;
		GD.Print($"[经验] 获得 {exp} 经验，当前 Exp: {Exp}/{ExpToNextLevel}");
		while (Exp >= ExpToNextLevel)
		{
			Exp -= ExpToNextLevel;
			LevelUp();
		}
		UpdateUI();
        SaveState();
	}

	private void LevelUp()
	{
		Level++;
		MaxHp += 20;
		Hp = MaxHp;
		BaseAttackPower++;
		SkillManager.Instance.AddSkillPoint();
		GD.Print($"★★★ 恭喜升级！等级：{Level}，血量：{MaxHp}，攻击：{BaseAttackPower}，技能点+1 ★★★");
        SaveState(); 
		RecalculateFinalStats();
	}

}
