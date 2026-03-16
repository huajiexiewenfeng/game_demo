using Godot;

/// <summary>
/// 外挂调试面板：按 F12 打开/关闭，可以实时调整经验倍率
/// </summary>
public partial class CheatPanel : CanvasLayer
{
    private Panel _panel;
    private Label _expLabel;
    private HSlider _expSlider;
    private Label _multiplierDisplay;
    private Label _statusLabel;

    public override void _Ready()
    {
        _panel = GetNode<Panel>("Panel");
        _expLabel = GetNode<Label>("Panel/VBox/ExpLabel");
        _expSlider = GetNode<HSlider>("Panel/VBox/ExpSlider");
        _multiplierDisplay = GetNode<Label>("Panel/VBox/MultiplierDisplay");
        _statusLabel = GetNode<Label>("Panel/VBox/StatusLabel");

        _panel.Visible = false;

        // 初始化滑块范围 1x ~ 100x
        _expSlider.MinValue = 1;
        _expSlider.MaxValue = 100;
        _expSlider.Step = 1;
        _expSlider.Value = GameManager.Instance?.ExpMultiplier ?? 1f;

        _expSlider.ValueChanged += OnSliderChanged;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo
            && keyEvent.Keycode == Key.F12)
        {
            _panel.Visible = !_panel.Visible;
            if (_panel.Visible) RefreshDisplay();
        }
    }

    private void OnSliderChanged(double value)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExpMultiplier = (float)value;
        }
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        float mul = GameManager.Instance?.ExpMultiplier ?? 1f;
        _multiplierDisplay.Text = $"当前经验倍率：×{mul:F0}";
        _expSlider.Value = mul;

        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (player != null)
        {
            _statusLabel.Text = $"玩家等级：{player.Level}  经验：{player.Exp}/{player.ExpToNextLevel}  攻击：{player.CurrentAttackPower}";
        }
    }
}
