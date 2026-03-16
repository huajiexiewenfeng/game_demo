using Godot;

/// <summary>
/// 技能数据模型：描述一个传奇技能的静态属性
/// </summary>
[GlobalClass]
public partial class SkillData : Resource
{
    public enum EffectType
    {
        ActiveDamage,    // 主动伤害技能（暴击/AOE 等）
        PassiveAttack,   // 被动：持续提升普通攻击
        PassiveDefense,  // 被动：增强防御/生命
        Heal,            // 治愈
    }

    [Export] public string SkillName { get; set; } = "未命名技能";
    [Export] public string Description { get; set; } = "";
    [Export] public EffectType Effect { get; set; } = EffectType.ActiveDamage;
    [Export] public int RequiredLevel { get; set; } = 1;  // 解锁所需等级
    [Export] public float Cooldown { get; set; } = 3.0f;  // 冷却时间（秒）
    [Export] public float DamageMultiplier { get; set; } = 1.5f; // 伤害倍率（相对于基础攻击力）
    [Export] public int PassiveBonusAtk { get; set; } = 0;   // 被动：永久加攻击
    [Export] public int PassiveBonusDef { get; set; } = 0;   // 被动：永久加防御
    [Export] public int PassiveBonusHp { get; set; } = 0;    // 被动：永久加最大血量
    [Export] public int HealAmount { get; set; } = 0;        // 治愈技能的回血量

    public SkillData() {}

    public SkillData(string name, string desc, EffectType effect, int reqLv,
        float cd = 3f, float dmgMul = 1f,
        int bonusAtk = 0, int bonusDef = 0, int bonusHp = 0, int heal = 0)
    {
        SkillName = name;
        Description = desc;
        Effect = effect;
        RequiredLevel = reqLv;
        Cooldown = cd;
        DamageMultiplier = dmgMul;
        PassiveBonusAtk = bonusAtk;
        PassiveBonusDef = bonusDef;
        PassiveBonusHp = bonusHp;
        HealAmount = heal;
    }

    // 技能的简短显示文字（用于 UI 列表）
    public string GetEffectDesc()
    {
        return Effect switch
        {
            EffectType.ActiveDamage  => $"⚔ 对目标造成 {DamageMultiplier:F1}x 攻击力",
            EffectType.PassiveAttack => $"✦ 永久攻击 +{PassiveBonusAtk}",
            EffectType.PassiveDefense => $"✦ 防御 +{PassiveBonusDef}  血量 +{PassiveBonusHp}",
            EffectType.Heal          => $"💚 恢复 {HealAmount} 点血量",
            _                        => ""
        };
    }
}
