using Godot;
using System.Collections.Generic;

/// <summary>
/// 底部技能快捷栏：显示已学技能和快捷键绑定，每帧更新冷却状态
/// </summary>
public partial class SkillHotbar : Control
{
    // 快捷栏最多5个格子（数字键 1-5）
    private const int MAX_SLOTS = 5;

    private HBoxContainer _slotRow;

    // 每个槽由两个 Label 组成：技能名 + 冷却
    private List<Panel> _slots = new List<Panel>();
    private List<Label> _nameLabels = new List<Label>();
    private List<Label> _cdLabels = new List<Label>();
    private List<Label> _keyLabels = new List<Label>();

    public override void _Ready()
    {
        _slotRow = GetNode<HBoxContainer>("SlotRow");
        BuildSlots();
    }

    private void BuildSlots()
    {
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            var panel = new Panel();
            panel.CustomMinimumSize = new Vector2(100, 60);

            var vbox = new VBoxContainer();
            vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.KeepSize, 4);

            var keyLbl = new Label();
            keyLbl.Text = $"[{i + 1}]";
            keyLbl.HorizontalAlignment = HorizontalAlignment.Center;
            keyLbl.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f));
            keyLbl.AddThemeFontSizeOverride("font_size", 11);
            vbox.AddChild(keyLbl);

            var nameLbl = new Label();
            nameLbl.Text = "空";
            nameLbl.HorizontalAlignment = HorizontalAlignment.Center;
            nameLbl.AddThemeFontSizeOverride("font_size", 11);
            nameLbl.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            vbox.AddChild(nameLbl);

            var cdLbl = new Label();
            cdLbl.Text = "";
            cdLbl.HorizontalAlignment = HorizontalAlignment.Center;
            cdLbl.AddThemeColorOverride("font_color", new Color(0.5f, 0.8f, 1f));
            cdLbl.AddThemeFontSizeOverride("font_size", 10);
            vbox.AddChild(cdLbl);

            panel.AddChild(vbox);
            _slotRow.AddChild(panel);

            _slots.Add(panel);
            _nameLabels.Add(nameLbl);
            _cdLabels.Add(cdLbl);
            _keyLabels.Add(keyLbl);
        }
    }

    // 每帧刷新技能槽显示（技能名 + 冷却）
    public override void _Process(double delta)
    {
        var sm = SkillManager.Instance;
        if (sm == null) return;

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (i < sm.LearnedSkills.Count)
            {
                var skill = sm.LearnedSkills[i];
                _nameLabels[i].Text = skill.SkillName;

                float cd = sm.GetCooldownRemaining(skill.SkillName);
                if (cd > 0)
                {
                    _cdLabels[i].Text = $"{cd:F1}s";
                    _nameLabels[i].AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
                }
                else
                {
                    _cdLabels[i].Text = "就绪";
                    _nameLabels[i].AddThemeColorOverride("font_color", Colors.White);
                }

                // 高亮激活状态背景
                _slots[i].SelfModulate = cd > 0
                    ? new Color(0.4f, 0.4f, 0.4f, 0.85f)
                    : new Color(0.15f, 0.15f, 0.45f, 0.95f);
            }
            else
            {
                _nameLabels[i].Text = "空";
                _cdLabels[i].Text = "";
                _slots[i].SelfModulate = new Color(0.2f, 0.2f, 0.2f, 0.7f);
                _nameLabels[i].AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.45f));
            }
        }
    }
}
