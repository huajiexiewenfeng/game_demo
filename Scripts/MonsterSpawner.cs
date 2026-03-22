using Godot;
using System;

[ScriptPath("res://Scripts/MonsterSpawner.cs")]
public partial class MonsterSpawner : Node2D
{
    [Export] public PackedScene EnemyScene { get; set; }
    public int   MaxEnemies     { get; set; } = 15;
    public float SpawnInterval  { get; set; } = 1.0f; 

    private float _spawnTimer = 0f;
    private Random _rand = new Random();
    
    private int _killQuota = 25;
    private bool _bossSpawned = false;
    private bool _dungeonCleared = false;
    private Enemy _bossRef = null;

    public override void _Ready()
    {
        if (EnemyScene == null)
            EnemyScene = GD.Load<PackedScene>("res://Scenes/Enemy.tscn");
            
        // 获取当前副本阶段（由主城大厅老兵点击后传送而来）
        int lv = GameManager.Instance.CurrentDungeonLevel;
        _killQuota = 25 + lv * 15; // 大幅增加击杀阈值，提升爽快感
        MaxEnemies = 25 + lv * 10; // 满屏暴兵
        SpawnInterval = 0.2f;      // 0.2秒极速刷怪
        
        // 全新副本地牢，重置杀敌计分板
        GameManager.Instance.CurrentDungeonKills = 0;
        
        GD.Print($"[副本核心] 远征第 {lv} 关地下城激活！清场阈值: {_killQuota}只怪物！");
    }

    public override void _Process(double delta)
    {
        if (EnemyScene == null || _dungeonCleared) return;

        float dt = (float)delta;
        _spawnTimer += dt;

        // —— 阶段 1：疯狂暴兵凑击杀数阶段 ——
        if (!_bossSpawned)
        {
            if (_spawnTimer >= SpawnInterval)
            {
                _spawnTimer = 0f;
                int count = GetTree().GetNodesInGroup("enemies").Count;
                if (count < MaxEnemies) SpawnEnemy(RandomMapPos(), false);
            }

            // 一旦达标，不再刷新杂鱼，立刻召唤关卡霸主
            if (GameManager.Instance.CurrentDungeonKills >= _killQuota)
            {
                _bossSpawned = true;
                SpawnBoss();
            }
        }
        // —— 阶段 2：击杀首领与最终结算 ——
        else if (_bossSpawned && !_dungeonCleared)
        {
            if (!GodotObject.IsInstanceValid(_bossRef) || _bossRef.Hp <= 0) // Boss 已阵亡！
            {
                _dungeonCleared = true;
                SpawnExtractionPortal();
            }
        }
    }

    private void SpawnEnemy(Vector2 pos, bool isBoss = false)
    {
        if (EnemyScene == null) return;

        var enemy = EnemyScene.Instantiate<Enemy>();
        enemy.GlobalPosition = pos;

        // 根据当前副本地形难度派发专有怪物基因族群
        int lv = GameManager.Instance.CurrentDungeonLevel;
        enemy.SpawnTypeIdx = DetermineSpawns(lv, isBoss);
        
        // 基于当前关卡等级增强怪物
        enemy.Hp += lv * 15;
        enemy.AttackDmg += lv * 2;
        
        // 视觉变异（随深度加深）
        if (lv > 1) {
            enemy.Modulate = new Color(1f - (lv*0.1f), 1f, 1f - (lv*0.1f));
        }

        GetParent().AddChild(enemy);
    }

    private void SpawnBoss()
    {
        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        Vector2 spawnPos = RandomMapPos();

        if (player != null)
        {
            float angle = (float)(_rand.NextDouble() * Math.PI * 2);
            float dist  = 350f; // 在屏幕边缘降临压迫感
            spawnPos = player.GlobalPosition + new Vector2(
                Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);
        }

        SpawnEnemy(spawnPos, true);
        
        // 由于 SpawnEnemy 已经被异步添加到场景树，找到刚刚那只做成 Boss 引用
        var nodes = GetTree().GetNodesInGroup("enemies");
        foreach(var node in nodes) {
            if (node is Enemy e && e.IsBoss) {
                _bossRef = e;
                break;
            }
        }

        int lv = GameManager.Instance.CurrentDungeonLevel;
        string[] bossNames = { "变异稻草人王", "变异钉耙猫王", "骷髅精灵", "尸王" };
        string bName = lv <= bossNames.Length ? bossNames[lv-1] : "远古魔神";

        // 播放 Boss 降临全屏血红震慑提示
        var hud = player?.GetNodeOrNull<CanvasLayer>("HUD");
        if (hud != null) {
            var warnObj = new Label();
            warnObj.Text = $"◆◆◆ 警告：击杀目标已达成，关底首领【{bName}】撕裂了空间！ ◆◆◆";
            warnObj.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
            warnObj.Position = new Vector2(1280f/2 - 350, 150);
            warnObj.AddThemeFontSizeOverride("font_size", 28);
            warnObj.AddThemeColorOverride("font_color", new Color(1.0f, 0.1f, 0.1f));
            warnObj.AddThemeColorOverride("font_outline_color", Colors.Black);
            warnObj.AddThemeConstantOverride("outline_size", 4);
            hud.AddChild(warnObj);
            
            var tw = warnObj.CreateTween();
            tw.TweenProperty(warnObj, "modulate:a", 0f, 4f).SetDelay(3f);
            tw.TweenCallback(Callable.From(() => warnObj.QueueFree()));
        }
    }

    private void SpawnExtractionPortal()
    {
        GD.Print("[副本结算] Boss 已受诛，安全区提取协议启动！");
        
        var portalInst = new ReturnPortal();
        var player = GetTree().GetFirstNodeInGroup("player") as Player;

        if (player != null)
        {
            // 在玩家脚下附近硬派召唤
            float angle = (float)(_rand.NextDouble() * Math.PI * 2);
            portalInst.Position = player.GlobalPosition + new Vector2(Mathf.Cos(angle) * 80f, Mathf.Sin(angle) * 80f); 
        }
        GetParent().AddChild(portalInst);
        
        // 胜利全屏横幅
        var hud = player?.GetNodeOrNull<CanvasLayer>("HUD");
        if (hud != null) {
            var winObj = new Label();
            winObj.Text = $"★ 秘境探险完成！请带上所有的战利品，踏入【蓝色传送阵】撤退！ ★";
            winObj.SetAnchorsPreset(Control.LayoutPreset.Center);
            winObj.Position = new Vector2(1280/2 - 380, 720/2 - 200);
            winObj.AddThemeFontSizeOverride("font_size", 24);
            winObj.AddThemeColorOverride("font_color", new Color(0.2f, 1f, 0.5f));
            winObj.AddThemeColorOverride("font_outline_color", Colors.Black);
            winObj.AddThemeConstantOverride("outline_size", 4);
            hud.AddChild(winObj);
        }
    }

    private int DetermineSpawns(int level, bool isBoss)
    {
        if (isBoss) {
            if (level == 1) return 3; // ScarecrowBoss
            if (level == 2) return 7; // CatBoss
            if (level == 3) return 12; // SkeletonBoss
            return 15; // ZombieKing
        }
        
        int r = GD.RandRange(0, 100);
        if (level == 1) {
            if (r < 40) return 0; // Chicken
            if (r < 75) return 1; // Deer
            return 2; // Scarecrow
        }
        if (level == 2) {
            if (r < 35) return 4; // HookCat
            if (r < 70) return 5; // RakeCat
            return 6; // HalfOrc
        }
        if (level == 3) {
            if (r < 30) return 8; // Skeleton
            if (r < 55) return 9; // SkeletonWarrior
            if (r < 80) return 10; // OrcSpearman
            return 11; // OrcBerserker
        }
        // Level 4
        if (r < 65) return 13; // Zombie
        return 14; // LightningZombie
    }

    private Vector2 RandomMapPos()
    {
        float x = (float)_rand.NextDouble() * 3000 - 500;
        float y = (float)_rand.NextDouble() * 3000 - 500;
        return new Vector2(x, y);
    }
}
