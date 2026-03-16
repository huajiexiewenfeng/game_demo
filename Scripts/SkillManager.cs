using Godot;
using System.Collections.Generic;

/// <summary>
/// 技能管理器：维护全局技能库、玩家已学会的技能列表、技能点余量
/// </summary>
public partial class SkillManager : Node
{
    public static SkillManager Instance { get; private set; }

    // 全部可学习的技能库（按等级排序）
    public List<SkillData> AllSkills { get; private set; } = new List<SkillData>();
    
    // 玩家已学会的技能
    public List<SkillData> LearnedSkills { get; private set; } = new List<SkillData>();
    
    // 剩余可用技能点
    public int SkillPoints { get; private set; } = 0;

    // 已学技能的冷却计时器（技能名 → 剩余CD）
    private Dictionary<string, float> _cooldowns = new Dictionary<string, float>();

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
            InitSkillBook();
        }
        else
        {
            QueueFree();
        }
    }

    public override void _Process(double delta)
    {
        // 每帧递减冷却
        var keys = new List<string>(_cooldowns.Keys);
        foreach (var k in keys)
        {
            _cooldowns[k] -= (float)delta;
            if (_cooldowns[k] <= 0) _cooldowns.Remove(k);
        }
    }

    // ====================================================
    //   传奇风格技能库（参考原版战士/法师技能）
    // ====================================================
    private void InitSkillBook()
    {
        // ── 等级 1 解锁 ──────────────────────────────────
        AllSkills.Add(new SkillData(
            "力劈", "挥出一刀，造成 160% 攻击力的单次伤害",
            SkillData.EffectType.ActiveDamage, reqLv: 1, cd: 4f, dmgMul: 1.6f));

        AllSkills.Add(new SkillData(
            "战斗意志", "战斗经验转化为力量，永久提升攻击力",
            SkillData.EffectType.PassiveAttack, reqLv: 1, cd: 0f,
            bonusAtk: 3));

        // ── 等级 2 解锁 ──────────────────────────────────
        AllSkills.Add(new SkillData(
            "铁皮功", "修炼铁皮，永久提升防御与最大血量",
            SkillData.EffectType.PassiveDefense, reqLv: 2, cd: 0f,
            bonusDef: 2, bonusHp: 30));

        // ── 等级 3 解锁 ──────────────────────────────────
        AllSkills.Add(new SkillData(
            "旋风斩", "以自身为圆心横扫，造成 200% 攻击力的范围伤害",
            SkillData.EffectType.ActiveDamage, reqLv: 3, cd: 6f, dmgMul: 2.0f));

        // ── 等级 4 解锁 ──────────────────────────────────
        AllSkills.Add(new SkillData(
            "精准刺击", "瞄准要害发动一击，造成 250% 攻击力的穿刺伤害",
            SkillData.EffectType.ActiveDamage, reqLv: 4, cd: 5f, dmgMul: 2.5f));

        AllSkills.Add(new SkillData(
            "金疮回气", "深呼运气，立即回复 80 点生命值（主动）",
            SkillData.EffectType.Heal, reqLv: 4, cd: 8f, heal: 80));

        // ── 等级 5 解锁 ──────────────────────────────────
        AllSkills.Add(new SkillData(
            "连环击", "快速连击 3 次，每次造成 120% 攻击力的伤害", 
            SkillData.EffectType.ActiveDamage, reqLv: 5, cd: 5f, dmgMul: 1.2f));

        AllSkills.Add(new SkillData(
            "怒气冲天", "沉浸怒火中，永久提升基础攻击力",
            SkillData.EffectType.PassiveAttack, reqLv: 5, cd: 0f, bonusAtk: 8));

        // ── 等级 7 解锁 ──────────────────────────────────
        AllSkills.Add(new SkillData(
            "霸体护甲", "锻造身体成为盾牌，大幅提升防御与血量",
            SkillData.EffectType.PassiveDefense, reqLv: 7, cd: 0f,
            bonusDef: 6, bonusHp: 80));

        // ── 等级 9 解锁 ──────────────────────────────────
        AllSkills.Add(new SkillData(
            "屠龙斩", "传奇绝技，造成 350% 攻击力的毁灭性打击",
            SkillData.EffectType.ActiveDamage, reqLv: 9, cd: 8f, dmgMul: 3.5f));

        // ── 等级 12 解锁 ─────────────────────────────────
        AllSkills.Add(new SkillData(
            "凤凰涅槃", "浴火重生，立即回复 300 点生命值",
            SkillData.EffectType.Heal, reqLv: 12, cd: 15f, heal: 300));

        GD.Print($"[技能系统] 技能书加载完毕，共 {AllSkills.Count} 个技能");
    }

    // ====================================================
    //   API：获得技能点（升级时调用）
    // ====================================================
    public void AddSkillPoint(int count = 1)
    {
        SkillPoints += count;
        GD.Print($"[技能] 获得技能点 +{count}，当前剩余：{SkillPoints}");
    }

    // ====================================================
    //   API：学习技能（消耗一个技能点）
    // ====================================================
    public bool TryLearnSkill(SkillData skill, int playerLevel)
    {
        if (SkillPoints <= 0)
        {
            GD.Print("[技能] 技能点不足！");
            return false;
        }
        if (playerLevel < skill.RequiredLevel)
        {
            GD.Print($"[技能] 等级不足，需要 {skill.RequiredLevel} 级才能学习【{skill.SkillName}】");
            return false;
        }
        if (LearnedSkills.Contains(skill))
        {
            GD.Print("[技能] 已学习过该技能！");
            return false;
        }

        LearnedSkills.Add(skill);
        SkillPoints--;
        GD.Print($"★ 学会技能【{skill.SkillName}】！剩余技能点：{SkillPoints}");

        // 立即应用被动效果
        ApplyPassive(skill);
        
        return true;
    }

    // ====================================================
    //   API：主动释放技能（如旋风斩、力劈等）
    // ====================================================
    public int TriggerSkill(SkillData skill, Player player, Enemy? target)
    {
        if (!LearnedSkills.Contains(skill))
        {
            GD.Print("[技能] 该技能尚未学习！");
            return 0;
        }
        if (_cooldowns.ContainsKey(skill.SkillName))
        {
            GD.Print($"[技能] 【{skill.SkillName}】冷却中：{_cooldowns[skill.SkillName]:F1}s");
            return 0;
        }
        if (skill.Effect == SkillData.EffectType.PassiveAttack ||
            skill.Effect == SkillData.EffectType.PassiveDefense)
        {
            GD.Print("[技能] 被动技能不需要主动释放");
            return 0;
        }

        int damage = 0;
        switch (skill.Effect)
        {
            case SkillData.EffectType.ActiveDamage:
                damage = (int)(player.CurrentAttackPower * skill.DamageMultiplier);
                target.TakeDamage(damage);
                GD.Print($"⚔ 使用【{skill.SkillName}】对 {target.MonsterName} 造成 {damage} 点伤害！");
                break;

            case SkillData.EffectType.Heal:
                player.Hp = Mathf.Min(player.Hp + skill.HealAmount, player.MaxHp);
                GD.Print($"💚 使用【{skill.SkillName}】恢复 {skill.HealAmount} 点生命值");
                break;
        }

        // 设置冷却
        _cooldowns[skill.SkillName] = skill.Cooldown;
        return damage;
    }

    // ====================================================
    //   内部：应用被动技能效果到玩家
    // ====================================================
    private void ApplyPassive(SkillData skill)
    {
        if (skill.Effect != SkillData.EffectType.PassiveAttack &&
            skill.Effect != SkillData.EffectType.PassiveDefense) return;

        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (player == null) return;

        if (skill.PassiveBonusAtk > 0)
        {
            player.BaseAttackPower += skill.PassiveBonusAtk;
            player.RecalculateStats();
            GD.Print($"[被动] {skill.SkillName}: 基础攻击 +{skill.PassiveBonusAtk}");
        }
        if (skill.PassiveBonusHp > 0)
        {
            player.MaxHp += skill.PassiveBonusHp;
            player.Hp = Mathf.Min(player.Hp + skill.PassiveBonusHp, player.MaxHp);
        }
    }

    public float GetCooldownRemaining(string skillName)
    {
        return _cooldowns.TryGetValue(skillName, out float cd) ? cd : 0f;
    }
}
