using Godot;
using System.Collections.Generic;

/// <summary>
/// 技能面板 UI：使用 B/K 快捷键或按钮打开，展示所有技能并支持点击学习
/// </summary>
public partial class SkillUI : Control
{
    private Panel _panel;
    private VBoxContainer _skillList;
    private Label _titleLabel;
    private Label _pointsLabel;

    public override void _Ready()
    {
        _panel = GetNode<Panel>("Panel");
        _skillList = GetNode<VBoxContainer>("Panel/ScrollContainer/SkillList");
        _titleLabel = GetNode<Label>("Panel/Title");
        _pointsLabel = GetNode<Label>("Panel/Points");
        _panel.Visible = false;
    }

    public void Toggle()
    {
        _panel.Visible = !_panel.Visible;
        if (_panel.Visible) Refresh();
    }

    public void Refresh()
    {
        foreach (Node child in _skillList.GetChildren()) child.QueueFree();

        var sm = SkillManager.Instance;
        if (sm == null) return;

        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        int playerLevel = player != null ? player.Level : 1;

        _pointsLabel.Text = $"可用技能点：{sm.SkillPoints}";

        foreach (SkillData skill in sm.AllSkills)
        {
            bool learned = sm.LearnedSkills.Contains(skill);
            bool canLearn = !learned && sm.SkillPoints > 0 && playerLevel >= skill.RequiredLevel;
            bool locked = playerLevel < skill.RequiredLevel;

            // 行容器
            var row = new HBoxContainer();
            row.CustomMinimumSize = new Vector2(0, 40);

            // 技能名 + 等级需求
            var nameLbl = new Label();
            nameLbl.Text = $"【{skill.SkillName}】(Lv.{skill.RequiredLevel})";
            nameLbl.CustomMinimumSize = new Vector2(160, 0);
            nameLbl.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;

            // 已学：绿色；解锁中：锁灰
            nameLbl.AddThemeColorOverride("font_color",
                learned ? new Color(0.4f, 1f, 0.4f) :
                locked  ? new Color(0.5f, 0.5f, 0.5f) :
                          Colors.White);
            row.AddChild(nameLbl);

            // 效果说明
            var descLbl = new Label();
            descLbl.Text = learned ? "✓ 已学" : skill.GetEffectDesc();
            descLbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.85f, 1f));
            descLbl.CustomMinimumSize = new Vector2(150, 0);
            descLbl.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
            row.AddChild(descLbl);

            // 学习按钮
            if (!learned && !locked)
            {
                var btn = new Button();
                btn.Text = "学习";
                btn.Disabled = !canLearn;
                var capturedSkill = skill; // 闭包捕获
                btn.Pressed += () => {
                    if (SkillManager.Instance.TryLearnSkill(capturedSkill, playerLevel))
                    {
                        Refresh(); // 刷新面板
                    }
                };
                row.AddChild(btn);
            }
            else if (locked)
            {
                var lockLbl = new Label();
                lockLbl.Text = "🔒";
                row.AddChild(lockLbl);
            }
            else
            {
                var okLbl = new Label();
                okLbl.Text = "✓";
                okLbl.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.4f));
                row.AddChild(okLbl);
            }

            _skillList.AddChild(row);
            _skillList.AddChild(new HSeparator());
        }
    }
}
