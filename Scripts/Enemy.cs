using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
    [Export] public string MonsterName  = "怪物";
    [Export] public int    Hp           = 50;
    [Export] public int    ExpReward    = 100;
    [Export] public float  MoveSpeed    = 55f;
    [Export] public float  ChaseSpeed   = 90f;
    [Export] public float  DetectRange  = 280f;
    [Export] public float  AttackRange  = 55f;
    [Export] public int    AttackDmg    = 5;
    [Export] public float  AttackCD     = 1.5f;
    [Export] public bool   IsBoss       = false;

    // 核心怪物族群定义分类（与关卡严丝合缝）
    public enum MonsterType {
        Chicken = 0, Deer = 1, Scarecrow = 2, ScarecrowBoss = 3,
        HookCat = 4, RakeCat = 5, HalfOrc = 6, CatBoss = 7,
        Skeleton = 8, SkeletonWarrior = 9, OrcSpearman = 10, OrcBerserker = 11, SkeletonBoss = 12,
        Zombie = 13, LightningZombie = 14, ZombieKing = 15
    }
    
    // 由 MonsterSpawner 创建时强行注入
    [Export] public int SpawnTypeIdx = -1;

    // 怪物间的排斥半径（防止叠堆）
    private const float SeparationRadius = 48f;
    private const float SeparationForce  = 200f;

    private enum State { Idle, Wander, Chase, Attack }
    private State _state = State.Idle;

    private bool        _isRanged = false;
    private int         _maxHp;
    private ProgressBar _healthBar;
    private Label       _nameLabel;
    private Sprite2D _sprite;
    private CharacterAnimator _animator; // 动画控制器

    private float   _stateTimer      = 0f;
    private float   _nextStateChange = 2f;
    private float   _attackTimer     = 0f;
    private Vector2 _wanderTarget    = Vector2.Zero;
    private Vector2 _knockbackVel    = Vector2.Zero;
    private Random  _rand            = new Random();


    private static System.Collections.Generic.Dictionary<string, Texture2D> _sharedTexCache = new System.Collections.Generic.Dictionary<string, Texture2D>();

    public override void _Ready()
    {
        InputPickable = true;
        AddToGroup("enemies");

        _sprite   = GetNodeOrNull<Sprite2D>("Sprite2D");
        _animator = GetNodeOrNull<CharacterAnimator>("Animator");
        
        // 全局基础缩放起点
        if (_sprite != null) _sprite.Scale = new Vector2(2f, 2f);
        
        // --- 构建强逻辑的 16 套生物生态字典装载 ---
        if (SpawnTypeIdx == -1) SpawnTypeIdx = (int)MonsterType.HalfOrc; // Editor backup
        MonsterType type = (MonsterType)SpawnTypeIdx;
        ApplyMonsterStats(type);
        
        _animator?.SyncOrigScale(); // 先同步新的真实 Scale，然后再绑定血条

        // 通用生成头顶名字 Label (所有人都有名字显示)
        _nameLabel = new Label();
        _nameLabel.Text = IsBoss ? $"★ {MonsterName} ★" : MonsterName;
        _nameLabel.AddThemeColorOverride("font_color", IsBoss ? new Color(1f, 0.5f, 0f) : Colors.White);
        _nameLabel.AddThemeFontSizeOverride("font_size", IsBoss ? 16 : 12);
        _nameLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _nameLabel.AddThemeConstantOverride("outline_size", 4);
        _nameLabel.Position = IsBoss ? new Vector2(-70, -130) : new Vector2(-40, -40); // 头顶偏移
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _nameLabel.CustomMinimumSize = new Vector2(80, 20);
        AddChild(_nameLabel);
        
        _maxHp = Hp; 

        // 绑定血条
        _healthBar = GetNodeOrNull<ProgressBar>("HealthBar");
        if (_healthBar != null)
        {
            _healthBar.MaxValue = _maxHp;
            _healthBar.Value    = Hp;
        }

        // 随机初始状态偏移，防止所有怪同步
        _nextStateChange = 1f + (float)_rand.NextDouble() * 3f;
    }

    private void ApplyMonsterStats(MonsterType type)
    {
        var shMat = new ShaderMaterial();
        shMat.Shader = GD.Load<Shader>("res://Scripts/black_transparent.gdshader");

        switch (type)
        {
            case MonsterType.Chicken:
                MonsterName = "鸡"; Hp = 8; ExpReward = 5; MoveSpeed = 30f; ChaseSpeed = 50f; AttackDmg = 1; AttackCD = 2.5f;
                SetSprite("res://Sprites/chicken.png", 0.08f, Colors.White, shMat); break;
            case MonsterType.Deer:
                MonsterName = "鹿"; Hp = 30; ExpReward = 15; MoveSpeed = 40f; ChaseSpeed = 85f; AttackDmg = 5;
                SetSprite("res://Sprites/deer.png", 0.12f, Colors.White, shMat); break;
            case MonsterType.Scarecrow:
                MonsterName = "稻草人"; Hp = 45; ExpReward = 25; MoveSpeed = 35f; ChaseSpeed = 60f; AttackDmg = 8;
                SetSprite("res://Sprites/scarecrow.png", 0.12f, Colors.White, shMat); break;
            case MonsterType.ScarecrowBoss:
                MonsterName = "变异稻草人王"; IsBoss = true; Hp = 350; ExpReward = 400; ChaseSpeed = 70f; AttackDmg = 25; AttackRange = 80f;
                SetSprite("res://Sprites/scarecrow.png", 0.25f, new Color(1.5f, 0.4f, 0.4f), shMat); break;

            case MonsterType.HookCat:
                MonsterName = "多钩猫"; Hp = 65; ExpReward = 45; MoveSpeed = 60f; ChaseSpeed = 110f; AttackDmg = 10;
                SetSprite("res://Sprites/cat_hook.png", 0.12f, Colors.White, shMat); break;
            case MonsterType.RakeCat:
                MonsterName = "钉耙猫"; Hp = 70; ExpReward = 50; MoveSpeed = 60f; ChaseSpeed = 100f; AttackDmg = 12;
                SetSprite("res://Sprites/cat_rake.png", 0.12f, Colors.White, shMat); break;
            case MonsterType.HalfOrc:
                MonsterName = "半兽人"; Hp = 90; ExpReward = 65; MoveSpeed = 50f; ChaseSpeed = 90f; AttackDmg = 15;
                break; // 原版贴图保留
            case MonsterType.CatBoss:
                MonsterName = "变异钉耙猫王"; IsBoss = true; Hp = 650; ExpReward = 900; ChaseSpeed = 130f; AttackDmg = 35; AttackCD = 1.0f; AttackRange = 90f;
                SetSprite("res://Sprites/cat_rake.png", 0.25f, new Color(0.4f, 0.8f, 1.5f), shMat); break;

            case MonsterType.Skeleton:
                MonsterName = "骷髅"; Hp = 130; ExpReward = 110; MoveSpeed = 40f; ChaseSpeed = 75f; AttackDmg = 20;
                SetSprite("res://Sprites/skeleton.png", 0.13f, Colors.White, shMat); break;
            case MonsterType.SkeletonWarrior:
                MonsterName = "骷髅战将"; Hp = 200; ExpReward = 160; MoveSpeed = 55f; ChaseSpeed = 120f; AttackDmg = 35;
                SetSprite("res://Sprites/skeleton_warrior.png", 0.14f, new Color(1f, 0.9f, 0.8f), shMat); break;
            case MonsterType.OrcSpearman:
                MonsterName = "半兽人掷矛手"; _isRanged = true; AttackRange = 250f; Hp = 110; ExpReward = 130; AttackDmg = 25;
                SetSprite("res://Sprites/orc_ranged.png", 0.12f, new Color(0.8f, 1.0f, 0.8f), shMat); break;
            case MonsterType.OrcBerserker:
                MonsterName = "半兽人狂战士"; Hp = 160; ExpReward = 150; ChaseSpeed = 160f; AttackDmg = 30;
                SetSprite("res://Sprites/orc_fast.png", 0.12f, new Color(1.0f, 0.8f, 0.8f), shMat); break;
            case MonsterType.SkeletonBoss:
                MonsterName = "骷髅精灵"; IsBoss = true; Hp = 1400; ExpReward = 2000; ChaseSpeed = 150f; AttackDmg = 55; AttackCD = 0.8f; AttackRange = 90f;
                SetSprite("res://Sprites/skeleton_warrior.png", 0.26f, new Color(2f, 0.2f, 0.2f), shMat); break;

            case MonsterType.Zombie:
                MonsterName = "普通僵尸"; Hp = 300; ExpReward = 320; MoveSpeed = 45f; ChaseSpeed = 65f; AttackDmg = 45;
                SetSprite("res://Sprites/zombie.png", 0.14f, Colors.White, shMat); break;
            case MonsterType.LightningZombie:
                MonsterName = "闪电僵尸"; _isRanged = true; AttackRange = 300f; Hp = 230; ExpReward = 360; AttackDmg = 65; AttackCD = 2.0f;
                SetSprite("res://Sprites/zombie_lightning.png", 0.14f, new Color(0.8f, 0.8f, 1.5f), shMat); break;
            case MonsterType.ZombieKing:
                MonsterName = "尸王"; IsBoss = true; Hp = 3500; ExpReward = 6000; ChaseSpeed = 140f; AttackDmg = 120; AttackRange = 100f;
                SetSprite("res://Sprites/zombie.png", 0.3f, new Color(1.2f, 0.8f, 0f), shMat); break;
        }
    }

    private void SetSprite(string path, float scale, Color mod, ShaderMaterial mat)
    {
        if (_sprite == null) return;
        _sprite.Texture = LoadTextureRobust(path);
        _sprite.Hframes = 1; _sprite.Vframes = 1;
        _sprite.Scale = new Vector2(scale, scale);
        _sprite.Modulate = mod;
        _sprite.Material = mat;
    }

    // ══════════════════════════════════════════
    //   AI 状态机
    // ══════════════════════════════════════════
    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _stateTimer  += dt;
        _attackTimer += dt;

        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        float distToPlayer = player != null
            ? GlobalPosition.DistanceTo(player.GlobalPosition)
            : float.MaxValue;

        // -- 状态机逻辑 --
        Vector2 moveVel = Vector2.Zero;

        switch (_state)
        {
            case State.Idle:
                if (distToPlayer < DetectRange)
                    _state = State.Chase;
                else if (_stateTimer >= _nextStateChange)
                {
                    _state = State.Wander;
                    _stateTimer = 0f;
                    _nextStateChange = 2f + (float)_rand.NextDouble() * 3f;
                    float angle = (float)(_rand.NextDouble() * Math.PI * 2);
                    float dist  = 80f + (float)_rand.NextDouble() * 150f;
                    _wanderTarget = GlobalPosition + new Vector2(
                        Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);
                }
                break;

            case State.Wander:
                if (distToPlayer < DetectRange)
                { _state = State.Chase; break; }

                var toWander = _wanderTarget - GlobalPosition;
                if (toWander.Length() < 8f || _stateTimer >= _nextStateChange)
                {
                    _state = State.Idle;
                    _stateTimer = 0f;
                    _nextStateChange = 1f + (float)_rand.NextDouble() * 2f;
                }
                else
                {
                    moveVel = toWander.Normalized() * MoveSpeed;
                }
                break;

            case State.Chase:
                if (player == null || distToPlayer > DetectRange * 1.3f)
                { _state = State.Idle; _stateTimer = 0f; break; }

                if (distToPlayer <= AttackRange)
                { _state = State.Attack; _stateTimer = 0f; break; }

                moveVel = GlobalPosition.DirectionTo(player.GlobalPosition) * ChaseSpeed;
                break;

            case State.Attack:
                if (player == null || distToPlayer > AttackRange * 1.6f)
                { _state = State.Chase; break; }

                if (_attackTimer >= AttackCD)
                {
                    _attackTimer = 0f;
                    if (_isRanged) ShootProjectileAtPlayer(player);
                    else DealDamageToPlayer(player);
                }
                break;
        }

        // ── 怪物间分离力（核心修复：防止叠堆在玩家身上）──────────────
        var sep = CalcSeparationVelocity();
        moveVel += sep;

        if (_knockbackVel.LengthSquared() > 10f)
        {
            _knockbackVel = _knockbackVel.Lerp(Vector2.Zero, 12f * (float)delta);
            moveVel += _knockbackVel;
        }
        // ─────────────────────────────────────────────────────────────

        Velocity = moveVel;
        MoveAndSlide();

        // 更新动画
        bool isAttacking = (_state == State.Attack);
        bool isMoving    = Velocity.Length() > 5f;
        _animator?.Update((float)delta, isMoving, isAttacking, Velocity);
    }

    /// <summary>
    /// 计算与周围其他怪物的排斥速度，让怪物形成包围圈而非叠堆
    /// </summary>
    private Vector2 CalcSeparationVelocity()
    {
        var push = Vector2.Zero;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node == this || node is not Enemy other) continue;
            float d = GlobalPosition.DistanceTo(other.GlobalPosition);
            if (d < SeparationRadius && d > 0.1f)
            {
                // 越近推力越强
                float strength = (SeparationRadius - d) / SeparationRadius;
                push += GlobalPosition.DirectionTo(other.GlobalPosition) * (-SeparationForce * strength);
            }
        }
        return push;
    }

    // ══════════════════════════════════════════
    //   点击选中
    // ══════════════════════════════════════════
    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton m && m.Pressed && m.ButtonIndex == MouseButton.Left)
        {
            GD.Print($"[选中] {MonsterName}");
            var player = GetTree().GetFirstNodeInGroup("player") as Player;
            player?.SetTargetEnemy(this);
            GetViewport().SetInputAsHandled();
        }
    }

    // ══════════════════════════════════════════
    //   怪物打玩家
    // ══════════════════════════════════════════
    private Texture2D LoadTextureRobust(string path) {
        if (_sharedTexCache.TryGetValue(path, out var cached)) return cached;

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

            if (err == Error.Ok && img.GetWidth() > 0) {
                var t = ImageTexture.CreateFromImage(img);
                _sharedTexCache[path] = t;
                return t;
            }
        } catch { }
        return null;
    }

    private void ShootProjectileAtPlayer(Player player)
    {
        // 渲染发光法术飞弹
        var proj = new Sprite2D();
        
        Texture2D projTex = LoadTextureRobust("res://Sprites/fire_burst.png");
        
        proj.Texture = projTex;
        var mat = new ShaderMaterial();
        // 使用专门编写的 发光+去黑底 着色器，实现透明的纯净发光法术特效
        mat.Shader = GD.Load<Shader>("res://Scripts/black_transparent_add.gdshader");
        proj.Material = mat;
        
        proj.Scale = new Vector2(0.15f, 0.15f); // 优雅的法术球比例
        proj.Modulate = new Color(0.2f, 1f, 0.2f); // 幽绿色的邪恶魔法
        proj.GlobalPosition = this.GlobalPosition + new Vector2(0, -20);
        proj.ZIndex = 110;

        GetParent().AddChild(proj);

        float flyDuration = 0.4f;
        var tween = proj.CreateTween();
        Vector2 targetPos = player.GlobalPosition + new Vector2(0, -20); 
        
        // 飞行补间动画
        tween.TweenProperty(proj, "global_position", targetPos, flyDuration).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.Parallel().TweenProperty(proj, "rotation", Mathf.Pi * 4, flyDuration); // 一边飞一边转
        
        // 命中判定（如果命中的瞬间玩家还在位置范围内且或者存活才造成真实伤害）
        tween.TweenCallback(Callable.From(() => {
            if (GodotObject.IsInstanceValid(proj)) proj.QueueFree();
            
            if (GodotObject.IsInstanceValid(player) && player.Hp > 0)
            {
                DealDamageToPlayer(player);
            }
        }));
    }

    private void DealDamageToPlayer(Player player)
    {
        // 核心修复：引入装备防御力减伤系统，并彻底解决“不掉血”的神级状态重写Bug
        int actualDamage = Mathf.Max(1, AttackDmg - player.FinalDefense);
        player.Hp -= actualDamage;
        player.Hp  = Mathf.Max(player.Hp, 0);
        GD.Print($"[{MonsterName}] 攻击玩家，造成 {actualDamage} 伤害 (被护甲抵消了 {player.FinalDefense} 点)，玩家剩余血量: {player.Hp}");
        
        // 华丽打击反馈：玩家飙血红闪
        var spr = player.GetNodeOrNull<Sprite2D>("Sprite2D");
        if (spr != null) 
        {
            var tw = player.CreateTween();
            tw.TweenProperty(spr, "modulate", new Color(1f, 0.2f, 0.2f, 1f), 0.1f);
            tw.TweenProperty(spr, "modulate", Colors.White, 0.1f);
        }

        player.RecalculateStats(); // 触发 UI 的扣血刷新
        player.SaveState();        // ★★★ 必须硬保存入 GameManager 本地堆，否则此起彼伏的状态覆盖流会直接将血回满！ ★★★
    }

    // ══════════════════════════════════════════
    //   受伤 & 死亡
    // ══════════════════════════════════════════
    public void TakeDamage(int damage)
    {
        Hp -= damage;
        if (_healthBar != null) _healthBar.Value = Hp;

        var dmgScene = GD.Load<PackedScene>("res://Scenes/DamageText.tscn");
        if (dmgScene != null)
        {
            var dmgNode = dmgScene.Instantiate<DamageText>();
            GetParent().AddChild(dmgNode);
            dmgNode.Init(damage, GlobalPosition, IsBoss ? new Color(1f, 0.5f, 0f) : new Color(1, 0, 0, 1));
        }

        // --- 打击感：受击闪白 ---
        if (_sprite != null)
        {
            var flashTween = CreateTween();
            var origColor = _sprite.Modulate;
            _sprite.Modulate = new Color(2f, 2f, 2f, 1f); // 超亮白
            flashTween.TweenProperty(_sprite, "modulate", origColor, 0.15f);
        }

        // --- 打击感：受击击退 ---
        var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (player != null)
        {
            Vector2 dir = player.GlobalPosition.DirectionTo(GlobalPosition);
            _knockbackVel = dir * 250f; // 瞬间击退速度
        }

        if (Hp <= 0) Die();
        else         _state = State.Chase;
    }

    private void Die()
    {
        int finalExp = (int)(ExpReward * GameManager.Instance.ExpMultiplier);
        GD.Print($"[{MonsterName}] 死亡！经验: {finalExp}");
        
        GameManager.Instance.CurrentDungeonKills++; // 推送击杀配额！
        GameManager.Instance.RandomDrop(GlobalPosition);

        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (player != null)
        {
            player.GainExp(finalExp);
            player.ClearTarget();
        }
        QueueFree();
    }
}
