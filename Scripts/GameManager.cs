using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }
    
    // 游戏"物品图鉴/数据库"
    public Dictionary<string, ItemData> ItemDatabase = new Dictionary<string, ItemData>();
    
    // 玩家背包
    public List<ItemData> PlayerInventory = new List<ItemData>();
    
    // ====================================================
    //   玩家持久化状态 (跨场景无缝切换的核心)
    // ====================================================
    public int PlayerLevel = 1;
    public int PlayerExp = 0;
    public int PlayerHp = 100;
    public int PlayerMaxHp = 100;
    public int PlayerBaseAtk = 10;
    
    // 传奇 6大神装独立槽位
    public string EquippedWeapon = "";
    public string EquippedArmor = "";
    public string EquippedHelmet = "";
    public string EquippedNecklace = "";
    public string EquippedBracelet = "";
    public string EquippedRing = "";
    
    // ====================================================
    //   副本进度系统 (Dungeon State Tracker)
    // ====================================================
    public int CurrentDungeonLevel = 1;
    public int CurrentDungeonKills = 0;

    // 外挂面板用：经验倍率（默认 1 倍，外挂面板可调整）
    public float ExpMultiplier = 1f;

    // 可掉落物品列表（按权重随机抽取）
    private List<(string ItemName, int Weight)> _dropTable = new List<(string, int)>();

    public override void _EnterTree()
    {
        if (Instance == null) 
        {
            Instance = this;
            InitItemDatabase();
            InitDropTable();

            // 黑科技：一键净化 AI 生成图片的“假透明白框/灰底格子”
            PurifyAiGenSprites();
        } 
        else 
        {
            QueueFree();
        }
    }

    // ====================================================
    //   传奇经典装备数据库
    // ====================================================
    private void InitItemDatabase()
    {
        // ─── 武器（剑） ───────────────────────────────
        Add("木剑",       ItemData.ItemType.Weapon,  atk: 2,  reqLv: 1);
        Add("乌木剑",     ItemData.ItemType.Weapon,  atk: 5,  reqLv: 3);
        Add("青铜剑",     ItemData.ItemType.Weapon,  atk: 8,  reqLv: 5);
        Add("铁剑",       ItemData.ItemType.Weapon,  atk: 12, reqLv: 8);
        Add("龙纹剑",     ItemData.ItemType.Weapon,  atk: 18, reqLv: 12);
        Add("屠龙剑",     ItemData.ItemType.Weapon,  atk: 25, reqLv: 16);
        Add("蛇形剑",     ItemData.ItemType.Weapon,  atk: 32, reqLv: 20);
        Add("骷髅权杖",   ItemData.ItemType.Weapon,  atk: 10, reqLv: 8);
        Add("大法师权杖", ItemData.ItemType.Weapon,  atk: 20, reqLv: 16);
        Add("神秘法杖",   ItemData.ItemType.Weapon,  atk: 30, reqLv: 22);
        
        // ─── 防具（上衣） ─────────────────────────────
        Add("布衣(男)",   ItemData.ItemType.Armor,   def: 2,  hp: 10,  reqLv: 1);
        Add("皮甲",       ItemData.ItemType.Armor,   def: 5,  hp: 20,  reqLv: 5);
        Add("铁甲",       ItemData.ItemType.Armor,   def: 10, hp: 40,  reqLv: 10);
        Add("精灵皮甲",   ItemData.ItemType.Armor,   def: 8,  hp: 30,  reqLv: 8);
        Add("玄武铠",     ItemData.ItemType.Armor,   def: 15, hp: 60,  reqLv: 15);
        Add("龙鳞甲",     ItemData.ItemType.Armor,   def: 22, hp: 80,  reqLv: 20);
        Add("神圣铠甲",   ItemData.ItemType.Armor,   def: 30, hp: 100, reqLv: 26);
        
        // ─── 头盔 ────────────────────────────────────
        Add("皮帽",       ItemData.ItemType.Helmet,  def: 1,  hp: 5,   reqLv: 1);
        Add("铁盔",       ItemData.ItemType.Helmet,  def: 4,  hp: 15,  reqLv: 8);
        Add("龙翼盔",     ItemData.ItemType.Helmet,  def: 8,  hp: 30,  reqLv: 15);
        Add("将军盔",     ItemData.ItemType.Helmet,  def: 12, hp: 45,  reqLv: 20);
        
        // ─── 项链 ────────────────────────────────────
        Add("铜项链",     ItemData.ItemType.Necklace, def: 1,  hp: 8,  reqLv: 1);
        Add("银项链",     ItemData.ItemType.Necklace, def: 2, hp: 20,  reqLv: 10);
        Add("金项链",     ItemData.ItemType.Necklace, def: 3, hp: 35,  reqLv: 18);
        Add("神圣项链",   ItemData.ItemType.Necklace, def: 5,  hp: 60, reqLv: 25);
        
        // ─── 手镯 ────────────────────────────────────
        Add("铜手镯",     ItemData.ItemType.Bracelet, atk: 1,  reqLv: 1);
        Add("银手镯",     ItemData.ItemType.Bracelet, atk: 3,  reqLv: 10);
        Add("金手镯",     ItemData.ItemType.Bracelet, atk: 5,  reqLv: 18);
        Add("天剑传说",   ItemData.ItemType.Bracelet, atk: 10, reqLv: 25);
        
        // ─── 戒指 ────────────────────────────────────
        Add("铁戒指",     ItemData.ItemType.Ring,    atk: 1,   reqLv: 1);
        Add("寒冰戒指",   ItemData.ItemType.Ring,    atk: 3,   hp: 20, reqLv: 12);
        Add("火焰戒指",   ItemData.ItemType.Ring,    atk: 5,   hp: 30, reqLv: 18);
        Add("魔法戒指",   ItemData.ItemType.Ring,    atk: 8,   hp: 50, reqLv: 24);
        
        // ─── 消耗品 ───────────────────────────────────
        Add("金疮药(小)", ItemData.ItemType.Consumable); ItemDatabase["金疮药(小)"].HealAmount = 50;
        Add("金疮药(中)", ItemData.ItemType.Consumable); ItemDatabase["金疮药(中)"].HealAmount = 150;
        Add("金疮药(大)", ItemData.ItemType.Consumable); ItemDatabase["金疮药(大)"].HealAmount = 300;
        Add("复活草",     ItemData.ItemType.Consumable); ItemDatabase["复活草"].HealAmount = 999;
        
        GD.Print("[数据库] 物品图鉴加载完毕...");
    }

    private void Add(string name, ItemData.ItemType type, int atk = 0, int def = 0, int hp = 0, int mp = 0, int reqLv = 1)
    {
        ItemDatabase[name] = new ItemData(name, type, atk, def, hp, mp, reqLv);
    }

    // ====================================================
    //   掉落表（越靠前的越容易掉）
    // ====================================================
    private void InitDropTable()
    {
        // Weight 越大越容易掉，类似"爆率"
        _dropTable.Add(("金疮药(小)", 40));
        _dropTable.Add(("木剑",       20));
        _dropTable.Add(("乌木剑",     15));
        _dropTable.Add(("铁剑",       10));
        _dropTable.Add(("皮甲",       10));
        _dropTable.Add(("铁盔",        8));
        _dropTable.Add(("铜手镯",      7));
        _dropTable.Add(("铁戒指",      7));
        _dropTable.Add(("龙纹剑",      5));
        _dropTable.Add(("玄武铠",      4));
        _dropTable.Add(("金疮药(中)",  4));
        _dropTable.Add(("龙翼盔",      3));
        _dropTable.Add(("屠龙剑",      2));
        _dropTable.Add(("龙鳞甲",      2));
        _dropTable.Add(("神圣铠甲",    1));
        _dropTable.Add(("屠龙剑",      1));
    }

    // ====================================================
    //   怪物死亡动态爆装喷发网络 (爆！爆！爆！)
    // ====================================================
    public void RandomDrop(Vector2 position)
    {
        // 降低基础药水掉落，防止包满
        if (GD.Randf() > 0.6f) DropItem("金疮药(小)", position);
        if (GD.Randf() > 0.85f) DropItem("金疮药(中)", position);

        // 抽奖极品装备掉落：维持适度的中奖率
        if (GD.Randf() > 0.5f) return;

        int totalWeight = 0;
        foreach (var entry in _dropTable) totalWeight += entry.Weight;

        int roll = (int)(GD.Randf() * totalWeight);
        int acc = 0;
        foreach (var entry in _dropTable)
        {
            acc += entry.Weight;
            if (roll < acc)
            {
                DropItem(entry.ItemName, position);
                
                // 终极双黄蛋暴击判定 (10%概率再爆一件神装！)
                if (GD.Randf() > 0.9f) DropItem(entry.ItemName, position);
                return;
            }
        }
    }

    public void DropItem(string itemName, Vector2 position)
    {
        if (!ItemDatabase.ContainsKey(itemName)) return;
        GD.Print($"[大爆引擎] 实体爆出：【{itemName}】！万众瞩目！");

        // 构造实体对象
        var loot = new LootEntity();
        loot.ItemName = itemName;
        loot.GlobalPosition = position;
        
        // 直接降临在主世界树根部，避免被随从父节点消灭
        var player = GetTree().GetFirstNodeInGroup("player") as Node;
        if (player != null && player.GetParent() != null) {
            player.GetParent().CallDeferred("add_child", loot);
        } else {
            CallDeferred("add_child", loot);
        }
    }

    public void AddToInventory(string itemName, bool autoEquip = true)
    {
        if (ItemDatabase.TryGetValue(itemName, out ItemData item))
        {
            PlayerInventory.Add(item);
            GD.Print($"[背包] 获得了 {item}！背包共 {PlayerInventory.Count} 件");
            
            // 全局通知已打开的背包直接刷新
            var playerNode = GetTree().GetFirstNodeInGroup("player") as Node;
            if (playerNode != null) {
                var invUI = playerNode.GetNodeOrNull<Control>("HUD/InventoryUI");
                if (invUI != null && (bool)invUI.Get("visible")) {
                    invUI.Call("Refresh");
                }
            }
            
            // 新手辅助机制：仅仅在特定初期或者打怪刚打出时自动装备，一旦有装备面板应该改用双击
            var player = GetTree().GetFirstNodeInGroup("player") as Node; 
            if (autoEquip && player != null && item.Type == ItemData.ItemType.Weapon && string.IsNullOrEmpty(EquippedWeapon))
            {
                EquipItem(item);
            }
        }
    }

    public void EquipItem(ItemData item)
    {
        if (item == null) return;
        switch (item.Type)
        {
            case ItemData.ItemType.Weapon:
                if (!string.IsNullOrEmpty(EquippedWeapon)) AddToInventory(EquippedWeapon, false);
                EquippedWeapon = item.ItemName;
                break;
            case ItemData.ItemType.Armor:
                if (!string.IsNullOrEmpty(EquippedArmor)) AddToInventory(EquippedArmor, false);
                EquippedArmor = item.ItemName;
                break;
            case ItemData.ItemType.Helmet:
                if (!string.IsNullOrEmpty(EquippedHelmet)) AddToInventory(EquippedHelmet, false);
                EquippedHelmet = item.ItemName;
                break;
            case ItemData.ItemType.Necklace:
                if (!string.IsNullOrEmpty(EquippedNecklace)) AddToInventory(EquippedNecklace, false);
                EquippedNecklace = item.ItemName;
                break;
            case ItemData.ItemType.Bracelet:
                if (!string.IsNullOrEmpty(EquippedBracelet)) AddToInventory(EquippedBracelet, false);
                EquippedBracelet = item.ItemName;
                break;
            case ItemData.ItemType.Ring:
                if (!string.IsNullOrEmpty(EquippedRing)) AddToInventory(EquippedRing, false);
                EquippedRing = item.ItemName;
                break;
            default:
                return; // 消耗品或材料无法穿戴
        }
        PlayerInventory.Remove(item);
        
        GD.Print($"[穿戴系统] 成功穿戴装备：{item.ItemName}！");
        
        var player = GetTree().GetFirstNodeInGroup("player") as Node;
        if (player != null && player.HasMethod("RecalculateFinalStats")) {
            player.Call("RecalculateFinalStats");
        }
    }

    public void UnequipItem(ItemData.ItemType slotType)
    {
        string removed = "";
        switch (slotType) {
            case ItemData.ItemType.Weapon:   removed = EquippedWeapon; EquippedWeapon = ""; break;
            case ItemData.ItemType.Armor:    removed = EquippedArmor; EquippedArmor = ""; break;
            case ItemData.ItemType.Helmet:   removed = EquippedHelmet; EquippedHelmet = ""; break;
            case ItemData.ItemType.Necklace: removed = EquippedNecklace; EquippedNecklace = ""; break;
            case ItemData.ItemType.Bracelet: removed = EquippedBracelet; EquippedBracelet = ""; break;
            case ItemData.ItemType.Ring:     removed = EquippedRing; EquippedRing = ""; break;
        }
        if (!string.IsNullOrEmpty(removed)) {
            GD.Print($"[穿戴系统] 卸下装备：{removed} 并放入背包！");
            AddToInventory(removed, false);
            
            var player = GetTree().GetFirstNodeInGroup("player") as Node;
            if (player != null && player.HasMethod("RecalculateFinalStats")) {
                player.Call("RecalculateFinalStats");
            }
        }
    }

    private void PurifyAiGenSprites()
    {
        string dirPath = "res://Sprites";
        using var dir = DirAccess.Open(dirPath);
        if (dir == null) return;
        
        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "") {
            if (fileName.EndsWith(".png")) {
                FloodFillCleanFakeAlpha(dirPath + "/" + fileName);
            }
            fileName = dir.GetNext();
        }
    }

    private void FloodFillCleanFakeAlpha(string resPath)
    {
        string globalPath = ProjectSettings.GlobalizePath(resPath);
        string flagPath = globalPath + ".cleaned";
        if (System.IO.File.Exists(flagPath)) return; // 被净化过了

        var img = new Image();
        if (img.Load(globalPath) != Error.Ok) return;

        int w = img.GetWidth();
        int h = img.GetHeight();
        bool changed = false;

        var visited = new bool[w, h];
        var q = new System.Collections.Generic.Queue<Vector2I>();

        // 边缘圈全量爆发
        for (int x = 0; x < w; x++) { q.Enqueue(new Vector2I(x, 0)); q.Enqueue(new Vector2I(x, h - 1)); visited[x, 0] = true; visited[x, h - 1] = true; }
        for (int y = 0; y < h; y++) { q.Enqueue(new Vector2I(0, y)); q.Enqueue(new Vector2I(w - 1, y)); visited[0, y] = true; visited[w - 1, y] = true; }

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            Color c = img.GetPixel(p.X, p.Y);

            float maxDiff = Mathf.Max(Mathf.Max(Mathf.Abs(c.R - c.G), Mathf.Abs(c.G - c.B)), Mathf.Abs(c.B - c.R));
            bool isFakeAlpha = (c.A < 0.1f) || (maxDiff < 0.1f && (c.R > 0.65f || c.R < 0.05f));

            if (isFakeAlpha)
            {
                img.SetPixel(p.X, p.Y, new Color(0, 0, 0, 0));
                changed = true;
                
                var neighbors = new Vector2I[] { 
                    new Vector2I(p.X + 1, p.Y), new Vector2I(p.X - 1, p.Y), 
                    new Vector2I(p.X, p.Y + 1), new Vector2I(p.X, p.Y - 1) 
                };
                foreach (var n in neighbors) {
                    if (n.X >= 0 && n.X < w && n.Y >= 0 && n.Y < h && !visited[n.X, n.Y]) {
                        visited[n.X, n.Y] = true;
                        q.Enqueue(n);
                    }
                }
            }
        }

        if (changed) {
            img.SavePng(globalPath); 
        }
        System.IO.File.WriteAllText(flagPath, "ok"); 
    }
}
