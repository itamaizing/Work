using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class MetabolismReptile : Skill
{
    [SerializeField] private Character _player;

    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private SpitPoison _spitPoison; 
 
    [SerializeField] private float _duration = 3f;

    private float _originalHpRegen;
    private float _originalManaRegen;
    private float _increaseManaRegen = 2f;
    private float _increaseHealthRegen = 2f;
    private float _increaseCastTime = 2f;
    private float _increaseCooldownTime = 2f;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        ApplyBuff();

        yield return null;
    }

    protected override void ClearData()
    {
    }

    private void ApplyBuff()
    {
        CmdIncreaseHealthRegen(_player.gameObject, _originalHpRegen, _increaseHealthRegen);
        CmdIncreaseManaRegen(_player.gameObject, _originalManaRegen, _increaseManaRegen);

        ReductionCooldownAndCastTimeSpells();

        Invoke(nameof(RemoveBuff), _duration);
    }

    private void RemoveBuff()
    {
        CmdRemoveHpRegen(_player.gameObject, _originalHpRegen);
        CmdRemoveManaRegen(_player.gameObject, _originalManaRegen);

        ResetCastTimeToBase();
    }

    private void ReductionCooldownAndCastTimeSpells()
    {
        float newRemainingCooldownForSpitPoison = _spitPoison.Cooldown.CooldownTime / _increaseCooldownTime;
        _spitPoison.Cooldown.SetReduced(newRemainingCooldownForSpitPoison, shouldModify: true);

        //Сделать потом уменьшение кулдаунов зарядов для PoisonBall

        _poisonBall.Buff.CastSpeed.ReductionPercentage(_increaseCastTime);
        _spitPoison.Buff.CastSpeed.ReductionPercentage(_increaseCastTime);
    }

    private void ResetCastTimeToBase()
    {
        _poisonBall.Buff.CastSpeed.IncreasePercentage(_increaseCastTime);
        _spitPoison.Buff.CastSpeed.IncreasePercentage(_increaseCastTime);
    }

    #region CommandMethods

    [Command]
    private void CmdIncreaseManaRegen(GameObject player, float originalManaRegen, float increaseManaRegen)
    {
        Character playerCharacter = player.GetComponent<Character>();
        playerCharacter.Resource.Attr_RegenValue.AddModifier(
            new AttributeModifier(increaseManaRegen, ModifierType.Percent, source: this));
    }

    [Command]
    private void CmdRemoveManaRegen(GameObject player, float originalManaRegen)
    {
        Character playerCharacter = player.GetComponent<Character>();
        playerCharacter.Resource.Attr_RegenValue.RemoveBySource(this, all: true);
    }

    [Command]
    private void CmdIncreaseHealthRegen(GameObject player, float originalHpRegen, float increaseHealthRegen)
    {
        Character playerCharacter = player.GetComponent<Character>();
        playerCharacter.Health.Attr_RegenValue.AddModifier(
            new AttributeModifier(increaseHealthRegen, ModifierType.Percent, source: this));
    }

    [Command]
    private void CmdRemoveHpRegen(GameObject player, float originalHealthRegen)
    {

        Character playerCharacter = player.GetComponent<Character>();

        playerCharacter.Health.Attr_RegenValue.RemoveBySource(this, all: true);
    }

    #endregion
}
