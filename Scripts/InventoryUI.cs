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

    public void Toggle()
    {
        if (!_panel.Visible)
        {
            _panel.Visible = true;
            Refresh();
            _panel.PivotOffset = _panel.Size / 2;
            _panel.Scale = new Vector2(0.5f, 0.5f);
            var tween = CreateTween();
            tween.TweenProperty(_panel, "scale", Vector2.One, 0.2f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }
        else
        {
            _panel.PivotOffset = _panel.Size / 2;
            var tween = CreateTween();
            tween.TweenProperty(_panel, "scale", new Vector2(0.5f, 0.5f), 0.15f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.In);
            tween.Chain().TweenCallback(Callable.From(() => _panel.Visible = false));
        }
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
            var itemPanel = new PanelContainer();
            var bgStyle = new StyleBoxFlat();
            bgStyle.BgColor = GetItemTypeColor(item.Type);
            bgStyle.ContentMarginLeft = 10;
            bgStyle.ContentMarginRight = 10;
            bgStyle.ContentMarginTop = 8;
            bgStyle.ContentMarginBottom = 8;
            bgStyle.CornerRadiusTopLeft = 4;
            bgStyle.CornerRadiusTopRight = 4;
            bgStyle.CornerRadiusBottomLeft = 4;
            bgStyle.CornerRadiusBottomRight = 4;
            itemPanel.AddThemeStyleboxOverride("panel", bgStyle);

            var row = new HBoxContainer();
            itemPanel.AddChild(row);
            
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

            // 核心交互：赋予每一行双击触发的灵魂能力
            var currentItem = item; // 捕获闭包变量
            itemPanel.MouseDefaultCursorShape = CursorShape.PointingHand; // 鼠标变小手
            itemPanel.GuiInput += (@event) => {
                if (@event is InputEventMouseButton m && m.Pressed && m.DoubleClick && m.ButtonIndex == MouseButton.Left) {
                    HandleItemDoubleClick(currentItem);
                }
            };

            _itemList.AddChild(itemPanel);

            // 分割线 (用边距代替真实的线，或者保留)
            var sep = new Control();
            sep.CustomMinimumSize = new Vector2(0, 4);
            _itemList.AddChild(sep);
        }
    }

    private Color GetItemTypeColor(ItemData.ItemType type)
    {
        return type switch {
            ItemData.ItemType.Weapon    => new Color(0.3f, 0.1f, 0.1f, 0.8f),
            ItemData.ItemType.Armor     => new Color(0.1f, 0.2f, 0.3f, 0.8f),
            ItemData.ItemType.Consumable=> new Color(0.1f, 0.3f, 0.1f, 0.8f),
            _                           => new Color(0.2f, 0.2f, 0.2f, 0.8f)
        };
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

    private void HandleItemDoubleClick(ItemData item)
    {
        var player = GetTree().GetFirstNodeInGroup("player") as Node; // 采用弱耦合获取
        if (player == null) return;

        if (item.Type == ItemData.ItemType.Consumable)
        {
            // 吃药系统
            int currentHp = (int)player.Get("Hp");
            int maxHp = (int)player.Get("FinalMaxHp");

            if (currentHp < maxHp) {
                int newHp = Mathf.Min(currentHp + item.HealAmount, maxHp);
                player.Set("Hp", newHp);
                GameManager.Instance.PlayerInventory.Remove(item);
                
                GD.Print($"[大补] 痛饮一瓶 {item.ItemName}！恢复了 {item.HealAmount} 点生命，当前 HP: {newHp}/{maxHp}");
                player.Call("UpdateUI"); 
                player.Call("SaveState");
                Refresh();
            } else {
                GD.Print("[提示] 你的生命值已经达到了巅峰，暂不需要服药！");
            }
        }
        else if (item.Type == ItemData.ItemType.Material)
        {
            GD.Print("[提示] 这些材料是用来打造神器的，暂且留着吧！");
        }
        else
        {
            // 极速穿戴全靠双击
            int pLevel = (int)player.Get("Level");
            if (pLevel < item.RequiredLevel) {
                GD.Print($"[严正拒绝] 修为不足！这件 {item.ItemName} 至少需要角色达到 Lv.{item.RequiredLevel} 方能驾驭！");
            } else {
                GameManager.Instance.EquipItem(item); // 内部已包含完美替换与入包逻辑
                Refresh();
                var eqUI = player.GetNodeOrNull<EquipmentUI>("HUD/EquipmentUI");
                eqUI?.Refresh();
            }
        }
    }
}
