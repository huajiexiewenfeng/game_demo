using Godot;
using System;

public partial class MonsterSpawner : Node2D
{
    [Export] public PackedScene EnemyScene { get; set; }
    [Export] public int   MaxEnemies     { get; set; } = 100;
    [Export] public float SpawnInterval  { get; set; } = 1.5f;  // 普通怪刷新间隔
    [Export] public float BossInterval   { get; set; } = 60f;   // Boss 刷新间隔（秒）

    private float _spawnTimer = 0f;
    private float _bossTimer  = 0f;
    private Random _rand = new Random();

    public override void _Ready()
    {
        if (EnemyScene == null)
            EnemyScene = GD.Load<PackedScene>("res://Scenes/Enemy.tscn");
    }

    public override void _Process(double delta)
    {
        if (EnemyScene == null) return;

        float dt = (float)delta;
        _spawnTimer += dt;
        _bossTimer  += dt;

        // —— 普通怪刷新 ——————————————————————
        if (_spawnTimer >= SpawnInterval)
        {
            _spawnTimer = 0f;
            int count = GetTree().GetNodesInGroup("enemies").Count;
            if (count < MaxEnemies)
                SpawnNormal();
        }

        // —— Boss 定时刷新 ————————————————————
        if (_bossTimer >= BossInterval)
        {
            _bossTimer = 0f;
            SpawnBoss();
        }
    }

    // 刷出普通半兽人
    private void SpawnNormal()
    {
        var enemy = EnemyScene.Instantiate<Enemy>();
        enemy.Position = RandomMapPos();
        GetParent().AddChild(enemy);
    }

    // 刷出 Boss：半兽人之王（在玩家附近）
    private void SpawnBoss()
    {
        var boss = EnemyScene.Instantiate<Enemy>();

        // 关键：必须在 AddChild 之前设置所有属性，因为 _Ready() 在 AddChild 时运行
        boss.MonsterName = "半兽人之王";
        boss.Hp          = 1000;
        boss.ExpReward   = 500;
        boss.MoveSpeed   = 45f;
        boss.ChaseSpeed  = 75f;
        boss.DetectRange = 450f;
        boss.AttackDmg   = 20;
        boss.AttackCD    = 1.2f;
        boss.AttackRange = 80f;
        boss.IsBoss      = true;

        // 在玩家周围 600-900 格处生成（确保玩家能看到 Boss 降临）
        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (player != null)
        {
            float angle = (float)(_rand.NextDouble() * Math.PI * 2);
            float dist  = 600f + (float)_rand.NextDouble() * 300f;
            boss.Position = player.GlobalPosition + new Vector2(
                Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);
        }
        else
        {
            boss.Position = RandomMapPos();
        }

        GetParent().AddChild(boss);
        GD.Print("★★★★★ [Boss] 半兽人之王 降临！准备战斗！");
    }

    // 在 -500 ~ 2500 的随机区域生成坐标
    private Vector2 RandomMapPos()
    {
        float x = (float)_rand.NextDouble() * 3000 - 500;
        float y = (float)_rand.NextDouble() * 3000 - 500;
        return new Vector2(x, y);
    }
}
