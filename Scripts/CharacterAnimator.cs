using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

[ScriptPath("res://Scripts/CharacterAnimator.cs")]
public partial class CharacterAnimator : Node
{

	private Sprite2D _sprite;

	private AnimatedSprite2D _animSprite;

	private Vector2 _origScale;

	private float _walkTimer = 0f;

	private float _idleTimer = 0f;

	private float _attackTimer = 0f;

	public override void _Ready()
	{
		Node2D parent = GetParent<Node2D>();
		_sprite = parent.GetNodeOrNull<Sprite2D>("Sprite2D");
		_animSprite = parent.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_sprite != null)
		{
			_origScale = _sprite.Scale;
            // 固定消除移动时的像素白边插值模糊
            _sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		}
		if (_animSprite != null)
		{
			_origScale = _animSprite.Scale;
            _animSprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		}
	}

    public void SyncOrigScale() 
    {
        if (_sprite != null) _origScale = _sprite.Scale;
        if (_animSprite != null) _origScale = _animSprite.Scale;
    }

	public void Update(float delta, bool moving, bool attacking, Vector2 velocity)
	{
		if (_attackTimer > 0f)
		{
			_attackTimer -= delta;
		}
		bool flag = _attackTimer > 0f;
		if (_animSprite != null)
		{
			string text = (flag ? "attack" : (moving ? "walk" : "idle"));
			if (_animSprite.Animation != (StringName)text)
			{
				_animSprite.Play(text);
			}
			if (velocity.X < -5f)
			{
				_animSprite.FlipH = true;
			}
			else if (velocity.X > 5f)
			{
				_animSprite.FlipH = false;
			}
		}
		else
		{
			if (_sprite == null)
			{
				return;
			}
			if (velocity.X < -5f)
			{
				_sprite.FlipH = true;
			}
			else if (velocity.X > 5f)
			{
				_sprite.FlipH = false;
			}
			if (flag)
			{
				float num = _attackTimer / 0.25f;
				if (_sprite.Hframes > 1) _sprite.Frame = ((num > 0.5f) ? 2 : 3);
				_sprite.Modulate = new Color(1f, 1f - num * 0.6f, 1f - num * 0.6f);
				_sprite.Scale = _origScale;
				return;
			}
            
            // Only reset once immediately after attacking so we don't block SkillManager tweens
            if (_attackTimer <= 0f && _attackTimer > -1f)
            {
                _sprite.Modulate = Colors.White;
                _attackTimer = -5f;
            }

			if (moving)
			{
				_walkTimer += delta * 12f; // Increased speed for clearer animation
				if (_sprite.Hframes > 1) _sprite.Frame = (int)_walkTimer % 2;
				
				// 纯整数像素对齐的呼吸效果 (Pixel-perfect bobbing)
				float bob = Mathf.Round(Mathf.Abs(Mathf.Sin(_walkTimer)) * -3f);
				
				_sprite.Position = new Vector2(0, bob);
				// 彻底禁止微浮点数旋转！因为 Godot 在 Nearest 像素采样时，斜向的 Quad 边缘会切到图集(Atlas)的透明缝隙和相邻帧，产生极其明显的1像素白边框闪烁！
				_sprite.Rotation = 0f; 
				_sprite.Scale = _origScale;
			}
			else
			{
				_sprite.Position = Vector2.Zero;
				_sprite.Rotation = 0f;
				if (_sprite.Hframes > 1) _sprite.Frame = 0;
				_idleTimer += delta * 1.8f;
				float num2 = 1f + Mathf.Sin(_idleTimer) * 0.03f;
				_sprite.Scale = _origScale * num2;
			}
		}
	}

	public void PlayAttack()
	{
		_attackTimer = 0.25f;
	}

}
