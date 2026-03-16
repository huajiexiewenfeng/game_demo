using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
    [Export] public string MonsterName  = "半兽人";
    [Export] public int    Hp           = 50;
    [Export] public int    ExpReward    = 100;
    [Export] public float  MoveSpeed    = 55f;
    [Export] public float  ChaseSpeed   = 90f;
    [Export] public float  DetectRange  = 280f;
    [Export] public float  AttackRange  = 55f;
    [Export] public int    AttackDmg    = 5;
    [Export] public float  AttackCD     = 1.5f;
    [Export] public bool   IsBoss       = false;

    // 怪物间的排斥半径（防止叠堆）
    private const float SeparationRadius = 48f;
    private const float SeparationForce  = 200f;

    private enum State { Idle, Wander, Chase, Attack }
    private State _state = State.Idle;

    private int         _maxHp;
    private ProgressBar _healthBar;
    private Label       _nameLabel;
    private Sprite2D _sprite;
    private CharacterAnimator _animator; // 动画控制器

    private float   _stateTimer      = 0f;
    private float   _nextStateChange = 2f;
    private float   _attackTimer     = 0f;
    private Vector2 _wanderTarget    = Vector2.Zero;
    private Random  _rand            = new Random();

    public override void _Ready()
    {
        InputPickable = true;
        _maxHp = Hp;
        AddToGroup("enemies");

        _sprite   = GetNodeOrNull<Sprite2D>("Sprite2D");
        _animator = GetNodeOrNull<CharacterAnimator>("Animator");

        // 绑定血条
        _healthBar = GetNodeOrNull<ProgressBar>("HealthBar");
        if (_healthBar != null)
        {
            _healthBar.MaxValue = _maxHp;
            _healthBar.Value    = Hp;
        }

        // Boss：放大 + 橙红色血条 + 名字标签
        if (IsBoss)
        {
            if (_sprite != null) _sprite.Scale *= 2.0f;
            // 改变血条颜色为橙色
            if (_healthBar != null)
            {
                _healthBar.AddThemeColorOverride("font_outline_color", new Color(1f, 0.4f, 0f));
            }

            // 在头顶添加名字 Label，方便玩家在地图上快速找到 Boss
            _nameLabel = new Label();
            _nameLabel.Text = $"★ {MonsterName} ★";
            _nameLabel.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0f));
            _nameLabel.AddThemeFontSizeOverride("font_size", 14);
            _nameLabel.Position = new Vector2(-50, -90); // 头顶偏移
            AddChild(_nameLabel);
        }

        // 随机初始状态偏移，防止所有怪同步
        _nextStateChange = 1f + (float)_rand.NextDouble() * 3f;
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
                    DealDamageToPlayer(player);
                }
                break;
        }

        // ── 怪物间分离力（核心修复：防止叠堆在玩家身上）──────────────
        var sep = CalcSeparationVelocity();
        moveVel += sep;
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
    private void DealDamageToPlayer(Player player)
    {
        player.Hp -= AttackDmg;
        player.Hp  = Mathf.Max(player.Hp, 0);
        GD.Print($"[{MonsterName}] 攻击玩家，造成 {AttackDmg} 伤害，玩家剩余血量: {player.Hp}");
        player.RecalculateStats();
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

        if (Hp <= 0) Die();
        else         _state = State.Chase;
    }

    private void Die()
    {
        int finalExp = (int)(ExpReward * GameManager.Instance.ExpMultiplier);
        GD.Print($"[{MonsterName}] 死亡！经验: {finalExp}");
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
