using System.Collections;
using Mirror;
using UnityEngine;

public class ConsumeComboHealOnDispelBooster : SkillTalentHandler
{
    private bool _enabled;

    private const float Duration = 9f;
    private const float TickInterval = 3f;
    private const float HealPercentPerEffect = 0.03f;
    
    public bool Enabled => _enabled;

    public ConsumeComboHealOnDispelBooster(NetworkBehaviour owner) : base(owner) { }

    public override void Enable(bool value) => _enabled = value;
    
    public void ApplyHealForOneEffect()
    {
        Owner.StartCoroutine(HealCoroutine());
    }

    private IEnumerator HealCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            yield return new WaitForSeconds(TickInterval);
            elapsed += TickInterval;

            var skill = Owner as ConsumeCombo_Scorpion;
            if (skill?.Hero?.Health == null) yield break;

            float healThisTick = skill.Hero.Health.MaxValue * (HealPercentPerEffect / 3f);

            Heal heal = new Heal
            {
                Value = healThisTick,
                DamageableSkill = null
            };

            skill.ApplyHeal(heal, skill.Hero.Health.gameObject, skill, nameof(ConsumeComboHealOnDispelBooster));
        }
    }
}