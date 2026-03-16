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
    //   怪物死亡随机掉落
    // ====================================================
    public void RandomDrop(Vector2 position)
    {
        // 50% 概率什么都不掉
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
                return;
            }
        }
    }

    public void DropItem(string itemName, Vector2 position)
    {
        if (!ItemDatabase.ContainsKey(itemName)) return;
        GD.Print($"[掉落] 爆出了：【{itemName}】");
        AddToInventory(itemName);
    }

    public void AddToInventory(string itemName)
    {
        if (ItemDatabase.TryGetValue(itemName, out ItemData item))
        {
            PlayerInventory.Add(item);
            GD.Print($"[背包] 获得了 {item}！背包共 {PlayerInventory.Count} 件");
            
            // 自动穿戴逻辑：找到玩家，穿上去
            var player = GetTree().GetFirstNodeInGroup("player") as Player;
            if (player != null && item.Type == ItemData.ItemType.Weapon)
            {
                // 只有攻击更高才自动替换
                if (player.EquippedWeapon == null || item.AttackBonus > player.EquippedWeapon.AttackBonus)
                {
                    player.EquipWeapon(item);
                }
            }
        }
    }
}
