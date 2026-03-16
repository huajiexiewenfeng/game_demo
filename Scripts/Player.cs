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

	public int ExpToNextLevel => Level * 100;

	public int CurrentAttackPower { get; private set; }

	public ItemData EquippedWeapon { get; private set; } = null;

	public override void _Ready()
	{
		targetPosition = base.GlobalPosition;
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
		AddToGroup("player");
		RecalculateStats();
	}

	public void RecalculateStats()
	{
		CurrentAttackPower = BaseAttackPower;
		if (EquippedWeapon != null)
		{
			CurrentAttackPower += EquippedWeapon.AttackBonus;
		}
		UpdateUI();
	}

	private void UpdateUI()
	{
		if (_levelLbl != null)
		{
			_levelLbl.Text = $"等级: {Level}";
		}
		if (_hpLbl != null)
		{
			_hpLbl.Text = $"生命值: {Hp}/{MaxHp}";
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
	}

	public void EquipWeapon(ItemData weapon)
	{
		EquippedWeapon = weapon;
		RecalculateStats();
		GD.Print($"[装备系统] 你装备了 {weapon.ItemName}，当前总攻击力变为：{CurrentAttackPower}");
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
		GD.Print($"[玩家] 挥动{((EquippedWeapon != null) ? EquippedWeapon.ItemName : "拳头")}砍向 {enemy.MonsterName}，造成 {CurrentAttackPower} 伤害！");
		enemy.TakeDamage(CurrentAttackPower);
		_animator?.PlayAttack();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: not false, Echo: false } inputEventKey && inputEventKey.Keycode == Key.B)
		{
			_inventoryUI?.Toggle();
			return;
		}
		if (@event is InputEventKey { Pressed: not false, Echo: false } inputEventKey2 && inputEventKey2.Keycode == Key.K)
		{
			_skillUI?.Toggle();
			return;
		}
		int num2;
		if (@event is InputEventKey { Pressed: not false, Echo: false, Keycode: var keycode })
		{
			if (1 == 0)
			{
			}
			Key num = keycode - 49;
			if ((ulong)num > 4uL)
			{
				goto IL_0104;
			}
			switch (num)
			{
			case Key.None:
				break;
			case (Key)1L:
				goto IL_00f0;
			case (Key)2L:
				goto IL_00f5;
			case (Key)3L:
				goto IL_00fa;
			case (Key)4L:
				goto IL_00ff;
			default:
				goto IL_0104;
			}
			num2 = 0;
			goto IL_0109;
		}
		goto IL_026f;
		IL_0109:
		if (1 == 0)
		{
		}
		int num3 = num2;
		if (num3 >= 0)
		{
			SkillManager instance = SkillManager.Instance;
			if (instance != null && num3 < instance.LearnedSkills.Count)
			{
				SkillData skillData = instance.LearnedSkills[num3];
				if (skillData.Effect == SkillData.EffectType.Heal)
				{
					instance.TriggerSkill(skillData, this, null);
					GD.Print($"[技能] 使用 [{num3 + 1}] {skillData.SkillName}");
				}
				else if (GodotObject.IsInstanceValid(targetEnemy))
				{
					instance.TriggerSkill(skillData, this, targetEnemy);
					GD.Print($"[技能] 使用 [{num3 + 1}] {skillData.SkillName} 对 {targetEnemy.MonsterName}");
				}
				else
				{
					GD.Print("[技能] 请先点击一个怪物作为目标！");
				}
			}
			return;
		}
		goto IL_026f;
		IL_0104:
		num2 = -1;
		goto IL_0109;
		IL_026f:
		if (@event is InputEventMouseButton { Pressed: not false } inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left && !_targetSetThisFrame)
		{
			ClearTarget();
			WalkTo(GetGlobalMousePosition());
		}
		return;
		IL_00ff:
		num2 = 4;
		goto IL_0109;
		IL_00f0:
		num2 = 1;
		goto IL_0109;
		IL_00f5:
		num2 = 2;
		goto IL_0109;
		IL_00fa:
		num2 = 3;
		goto IL_0109;
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
	}

	private void LevelUp()
	{
		Level++;
		MaxHp += 20;
		Hp = MaxHp;
		BaseAttackPower++;
		SkillManager.Instance.AddSkillPoint();
		GD.Print($"★★★ 恭喜升级！等级：{Level}，血量：{MaxHp}，攻击：{BaseAttackPower}，技能点+1 ★★★");
		RecalculateStats();
	}

}
