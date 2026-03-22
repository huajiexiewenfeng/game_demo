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
        
        // 动态调整面板大小以展示全部内容，去除滚动条 (大幅加宽以容纳快捷键槽)
        _panel.CustomMinimumSize = new Vector2(650, 520);
        _panel.Size = new Vector2(650, 520);
        
        var scroll = GetNode<ScrollContainer>("Panel/ScrollContainer");
        scroll.Size = new Vector2(640, 460);
        scroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        
        _titleLabel.CustomMinimumSize = new Vector2(640, 20);
        _pointsLabel.CustomMinimumSize = new Vector2(640, 20);

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

            // 根容器包装器
            var itemPanel = new PanelContainer();
            var bgStyle = new StyleBoxFlat();
            bgStyle.BgColor = learned ? new Color(0.1f, 0.4f, 0.1f, 0.7f) : 
                              locked  ? new Color(0.2f, 0.2f, 0.2f, 0.7f) : 
                                        new Color(0.1f, 0.2f, 0.4f, 0.7f);
            bgStyle.ContentMarginLeft = 10;
            bgStyle.ContentMarginRight = 10;
            bgStyle.ContentMarginTop = 8;
            bgStyle.ContentMarginBottom = 8;
            bgStyle.CornerRadiusTopLeft = 4;
            bgStyle.CornerRadiusTopRight = 4;
            bgStyle.CornerRadiusBottomLeft = 4;
            bgStyle.CornerRadiusBottomRight = 4;
            itemPanel.AddThemeStyleboxOverride("panel", bgStyle);

            // 行容器
            var row = new HBoxContainer();
            row.CustomMinimumSize = new Vector2(0, 40);
            itemPanel.AddChild(row);

            // 技能名 + 等级需求
            var nameLbl = new Label();
            nameLbl.Text = $"【{skill.SkillName}】(Lv.{skill.RequiredLevel})";
            nameLbl.CustomMinimumSize = new Vector2(130, 0); // 回调大幅度占据空间的最小锁死限制
            nameLbl.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;

            // 已学：绿色；解锁中：锁灰
            nameLbl.AddThemeColorOverride("font_color",
                learned ? new Color(0.6f, 1f, 0.6f) :
                locked  ? new Color(0.6f, 0.6f, 0.6f) :
                          Colors.White);
            row.AddChild(nameLbl);

            // 效果说明
            var descLbl = new Label();
            descLbl.Text = learned ? "✓ 已学" : skill.GetEffectDesc();
            descLbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.85f, 1f));
            descLbl.CustomMinimumSize = new Vector2(100, 0); // 彻底释放最小拉伸枷锁（交由 Expand 自行填满所有剩余间隙即可）
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

                // 只允许主动释放的法术/攻击绑定热键
                if (skill.Effect == SkillData.EffectType.ActiveDamage || skill.Effect == SkillData.EffectType.Heal)
                {
                    row.AddChild(new Control() { CustomMinimumSize = new Vector2(15, 0) }); // 间距

                    var bindTip = new Label();
                    bindTip.Text = "设快捷键:";
                    bindTip.AddThemeFontSizeOverride("font_size", 12);
                    bindTip.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
                    row.AddChild(bindTip);

                    for (int k = 0; k < 5; k++)
                    {
                        var kBtn = new Button();
                        kBtn.Text = (k + 1).ToString();
                        kBtn.CustomMinimumSize = new Vector2(28, 28);
                        kBtn.AddThemeFontSizeOverride("font_size", 12);
                        
                        // 如果当前槽位绑定的就是这个技能，高亮金框显示
                        if (sm.HotbarMapping.TryGetValue(k, out var boundSkill) && boundSkill == skill) {
                            kBtn.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0f));
                            kBtn.Text = $"[{k+1}]";
                        }

                        int slotK = k; // 本地捕获
                        var capSkill = skill; // 本地捕获
                        kBtn.Pressed += () => {
                            sm.BindSkillToKey(slotK, capSkill);
                            Refresh(); // 刷新界面高亮
                        };
                        row.AddChild(kBtn);
                    }
                }
            }

            _skillList.AddChild(itemPanel);
            
            var sep = new Control();
            sep.CustomMinimumSize = new Vector2(0, 4);
            _skillList.AddChild(sep);
        }
    }
}
