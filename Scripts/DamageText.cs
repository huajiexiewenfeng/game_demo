using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

[ScriptPath("res://Scripts/DamageText.cs")]
public partial class DamageText : Label
{

	[Export(PropertyHint.None, "")]
	public float FloatSpeed = 50f;

	[Export(PropertyHint.None, "")]
	public float LifeTime = 1f;

	private float _timer = 0f;

	public override void _Process(double delta)
	{
		base.Position -= new Vector2(0f, FloatSpeed * (float)delta);
		_timer += (float)delta;
		base.Modulate = new Color(base.Modulate.R, base.Modulate.G, base.Modulate.B, 1f - _timer / LifeTime);
		if (_timer >= LifeTime)
		{
			QueueFree();
		}
	}

	public void Init(int damage, Vector2 startPos, Color color)
	{
		base.Text = "-" + damage;
		base.Position = startPos + new Vector2(GD.RandRange(-20, 20), -30f);
		AddThemeColorOverride("font_color", color);
	}

}
