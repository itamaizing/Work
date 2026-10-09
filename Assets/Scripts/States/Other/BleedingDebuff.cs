using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BleedingDebuff : StateBasic
{
    private float _duration;
    private float _baseDuration;
    private float timer = 0;
    public override States State => States.Bleeding;
    
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => throw new System.NotImplementedException();

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Entering KnockdownDebuff State");
        characterState = character;

        //effects.Add(StatusEffect.Others);

        _duration = durationToExit;
        _baseDuration = durationToExit;
    }

    protected override void OnExit()
    {
        Debug.Log("Exiting KnockdownDebuff State");
    }

    public override void UpdateState()
    {
        Debug.Log("Updating KnockdownDebuff State");
        timer += Time.deltaTime;

        if (timer >= 1f)
        {
            DealDamage();
            timer = 0f;
        }
    }

    private void DealDamage()
    {
        Debug.LogWarning("Bleeding .DealDamage()");
        //characterState.GetComponent<HealthComponent>().CmdTryTakeDamage(Random.Range(1, 3), Info.DamageType.Magical, Info.AttackRangeType.MeleeAttack);
    }
}
