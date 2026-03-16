using Godot;
using System.Collections.Generic;

public partial class InventoryUI : Control
{
    // 面板本身（默认隐藏）
    private Panel _panel;
    // 动态内容列表的容器
    private VBoxContainer _itemList;
    // 顶部标题
    private Label _titleLabel;
    // 显示物品总数
    private Label _countLabel;

    public override void _Ready()
    {
        _panel = GetNode<Panel>("Panel");
        _itemList = GetNode<VBoxContainer>("Panel/ScrollContainer/ItemList");
        _titleLabel = GetNode<Label>("Panel/Title");
        _countLabel = GetNode<Label>("Panel/Count");
        
        // 默认关闭
        _panel.Visible = false;
    }

    // 供按钮或快捷键调用的开关方法
    public void Toggle()
    {
        _panel.Visible = !_panel.Visible;
        if (_panel.Visible) Refresh();
    }

    // 刷新面板内容（从 GameManager 背包中读取）
    public void Refresh()
    {
        // 清空旧内容
        foreach (Node child in _itemList.GetChildren()) child.QueueFree();
        
        List<ItemData> inventory = GameManager.Instance.PlayerInventory;
        _countLabel.Text = $"共 {inventory.Count} 件";
        
        if (inventory.Count == 0)
        {
            var empty = new Label();
            empty.Text = "背包里什么都没有...";
            _itemList.AddChild(empty);
            return;
        }

        // 按类型分组计数（方便显示"武器 × 3"等）
        foreach (ItemData item in inventory)
        {
            var row = new HBoxContainer();
            
            // 类型图标文字
            var typeLbl = new Label();
            typeLbl.Text = GetTypeIcon(item.Type);
            typeLbl.CustomMinimumSize = new Vector2(30, 0);
            row.AddChild(typeLbl);

            // 物品名称（如果是当前装备的武器，高亮显示）
            var nameLbl = new Label();
            var player = GetTree().GetFirstNodeInGroup("player") as Player;
            bool isEquipped = (player != null && player.EquippedWeapon == item);
            nameLbl.Text = isEquipped ? $"★ {item.ItemName}" : item.ItemName;
            nameLbl.AddThemeColorOverride("font_color", isEquipped ? new Color(1f, 0.9f, 0f) : Colors.White);
            nameLbl.CustomMinimumSize = new Vector2(100, 0);
            row.AddChild(nameLbl);

            // 属性简介
            var statLbl = new Label();
            string stats = "";
            if (item.AttackBonus > 0)  stats += $" 攻+{item.AttackBonus}";
            if (item.DefenseBonus > 0) stats += $" 防+{item.DefenseBonus}";
            if (item.HpBonus > 0)      stats += $" 血+{item.HpBonus}";
            if (item.HealAmount > 0)   stats += $" 回{item.HealAmount}血";
            statLbl.Text = stats.Length > 0 ? stats : "-";
            statLbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.9f, 1f));
            row.AddChild(statLbl);

            _itemList.AddChild(row);

            // 分割线
            var sep = new HSeparator();
            _itemList.AddChild(sep);
        }
    }

    private string GetTypeIcon(ItemData.ItemType type)
    {
        return type switch {
            ItemData.ItemType.Weapon    => "🗡",
            ItemData.ItemType.Armor     => "🛡",
            ItemData.ItemType.Helmet    => "⛑",
            ItemData.ItemType.Necklace  => "💎",
            ItemData.ItemType.Bracelet  => "🔮",
            ItemData.ItemType.Ring      => "💍",
            ItemData.ItemType.Consumable=> "🧪",
            _                           => "📦"
        };
    }
}
