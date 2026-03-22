using Godot;
using System.Collections.Generic;

[ScriptPath("res://Scripts/EquipmentUI.cs")]
public partial class EquipmentUI : Control
{
    private Panel _panel;
    private GridContainer _grid;
    
    public override void _Ready()
    {
        _panel = new Panel();
        _panel.Size = new Vector2(300, 480);
        // 基于标准 1280x720 屏幕放置在偏左部分，避免挡住中间视野和右侧背包
        _panel.Position = new Vector2(100, 80); 
        _panel.Visible = false;
        AddChild(_panel);
        
        // 绑定暗黑高定边框主题
        UIThemeManager.ApplyPremiumTheme(_panel);

        var title = new Label();
        title.Text = "★ 角色纸娃娃与装备 ★";
        title.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        title.Position = new Vector2(0, 20);
        title.CustomMinimumSize = new Vector2(300, 20);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _panel.AddChild(title);

        _grid = new GridContainer();
        _grid.Columns = 2; // 两列排列
        _grid.Position = new Vector2(25, 70);
        _grid.AddThemeConstantOverride("h_separation", 15);
        _grid.AddThemeConstantOverride("v_separation", 15);
        _panel.AddChild(_grid);
    }

    public void Toggle()
    {
        _panel.Visible = !_panel.Visible;
        if (_panel.Visible) Refresh();
    }

    public void Refresh()
    {
        foreach (Node child in _grid.GetChildren()) child.QueueFree();

        // 传奇经典 6 神装槽位
        var slots = new (string Label, string ItemName, ItemData.ItemType Type)[] {
            ("【武器】", GameManager.Instance.EquippedWeapon, ItemData.ItemType.Weapon),
            ("【盔甲】", GameManager.Instance.EquippedArmor, ItemData.ItemType.Armor),
            ("【头盔】", GameManager.Instance.EquippedHelmet, ItemData.ItemType.Helmet),
            ("【项链】", GameManager.Instance.EquippedNecklace, ItemData.ItemType.Necklace),
            ("【手镯】", GameManager.Instance.EquippedBracelet, ItemData.ItemType.Bracelet),
            ("【戒指】", GameManager.Instance.EquippedRing, ItemData.ItemType.Ring)
        };

        foreach (var slot in slots)
        {
            var btn = new Button();
            btn.CustomMinimumSize = new Vector2(115, 75);
            
            if (string.IsNullOrEmpty(slot.ItemName)) {
                btn.Text = $"{slot.Label}\n <极度空虚>";
                btn.Modulate = new Color(0.6f, 0.6f, 0.6f);
            } else {
                btn.Text = $"{slot.Label}\n{slot.ItemName}";
                // 极品色彩特效
                if (slot.ItemName.Contains("龙") || slot.ItemName.Contains("神圣")) btn.AddThemeColorOverride("font_color", new Color(1.0f, 0.5f, 0.1f)); 
                else if (slot.ItemName.Contains("剑") || slot.ItemName.Contains("铠")) btn.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 1.0f)); 
                else btn.AddThemeColorOverride("font_color", new Color(0.5f, 0.9f, 1.0f)); 
                
                // 双击卸下装备核心逻辑
                ItemData.ItemType slotType = slot.Type;
                btn.GuiInput += (@event) => {
                    if (@event is InputEventMouseButton m && m.Pressed && m.DoubleClick && m.ButtonIndex == MouseButton.Left) {
                        GameManager.Instance.UnequipItem(slotType);
                        Refresh();
                        var player = GetTree().GetFirstNodeInGroup("player") as Node;
                        player?.GetNodeOrNull<InventoryUI>("HUD/InventoryUI")?.Refresh();
                    }
                };
            }
            _grid.AddChild(btn);
        }
        
        // 角色战斗力数值总览结算板
        var statsLbl = _panel.GetNodeOrNull<Label>("StatsOverview");
        if (statsLbl == null) {
            statsLbl = new Label();
            statsLbl.Name = "StatsOverview";
            statsLbl.Position = new Vector2(30, 360);
            statsLbl.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0.2f));
            _panel.AddChild(statsLbl);
        }
        
        var p = GetTree().GetFirstNodeInGroup("player") as Node;
        if (p != null) { // Note: Cast as dynamic Node to pull properties
            int hp = (int)p.Get("FinalMaxHp");
            int def = (int)p.Get("FinalDefense");
            int atk = (int)p.Get("FinalAttackPower");
            int lv = (int)p.Get("Level");
            statsLbl.Text = $"◆ 宗师级战力雷达 ◆\n\n角色等级: {lv}\n生命值上限: {hp}\n极致防御力: {def}\n恐怖攻击力: {atk}";
        }
    }
}
