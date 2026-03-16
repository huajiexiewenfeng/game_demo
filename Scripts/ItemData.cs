using Godot;
using System;

// 继承自 Resource 可以让我们在 Godot 编辑器中直接创建和保存这种自定义的数据资源
[GlobalClass]
public partial class ItemData : Resource
{
    // 物品类型的枚举
    public enum ItemType { Weapon, Armor, Helmet, Necklace, Bracelet, Ring, Consumable, Material }

    [Export] public string ItemName { get; set; } = "未知物品";
    [Export] public ItemType Type { get; set; } = ItemType.Material;
    [Export] public int RequiredLevel { get; set; } = 1; // 穿戴所需等级

    // 基础属性加成（如果是装备）
    [Export] public int AttackBonus { get; set; } = 0;
    [Export] public int DefenseBonus { get; set; } = 0;
    [Export] public int HpBonus { get; set; } = 0;
    [Export] public int MpBonus { get; set; } = 0;
    
    // 如果是消耗品（如金疮药），回复的血量
    [Export] public int HealAmount { get; set; } = 0;
    
    // 图标路径预留（后续有了美术素材挂载在这里）
    [Export] public string IconPath { get; set; } = "";

    public ItemData() {}

    // 方便代码里快速创建物品
    public ItemData(string name, ItemType type, int atk = 0, int def = 0, int hp = 0, int mp = 0, int reqLv = 1)
    {
        ItemName = name;
        Type = type;
        AttackBonus = atk;
        DefenseBonus = def;
        HpBonus = hp;
        MpBonus = mp;
        RequiredLevel = reqLv;
    }

    public override string ToString()
    {
        string typeStr = Type switch {
            ItemType.Weapon => "武器",
            ItemType.Armor => "防具",
            ItemType.Helmet => "头盔",
            ItemType.Necklace => "项链",
            ItemType.Bracelet => "手镯",
            ItemType.Ring => "戒指",
            ItemType.Consumable => "消耗品",
            _ => "材料"
        };
        string stats = "";
        if (AttackBonus > 0) stats += $" 攻+{AttackBonus}";
        if (DefenseBonus > 0) stats += $" 防+{DefenseBonus}";
        if (HpBonus > 0) stats += $" 血+{HpBonus}";
        return $"[{typeStr}] {ItemName} (Lv.{RequiredLevel}){stats}";
    }
}
