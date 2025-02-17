using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class SpiritEnergyState : AbstractCharacterState
{
    private Skill _skill;

    private float _baseDuration;
    private float _duration;
    private bool _isTalentActive = false;

    private const float ManaRestorePerStack = 0.09f;
    private const float BuffedManaRestorePerStack = 0.18f;
    private const float BonusManaRestore = 0.05f;
    private const float BuffedBonusManaRestore = 1f;
    private const float HealthBonusPerStack = 1f;
    private const float DamageManaRestorePercent = 0.05f;

    private List<StatusEffect> _effects = new();
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.SpiritEnergy;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    private Health _healthComponent;
    private Resource _manaResource;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _skill = personWhoMadeBuff?.Abilities?.Abilities?.FirstOrDefault(o => o.Name == skillName);
        _characterState = character;
        _duration = durationToExit;
        _baseDuration = durationToExit;
        CurrentStacksCount++;
        MaxStacksCount = 2;
        _isTalentActive = damageToExit > 0;

        _healthComponent = character.GetComponent<Health>();
        _manaResource = character.Character.TryGetResource(ResourceType.Mana);

        if (_healthComponent != null)
        {
            _healthComponent.DamageTaken += OnDamageTaken;
        }

        var manaRestoreValue = _isTalentActive ? BuffedManaRestorePerStack : ManaRestorePerStack;
        ApplyManaRestore(manaRestoreValue * CurrentStacksCount);
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;

        if (_duration <= _baseDuration * (CurrentStacksCount - 1) && CurrentStacksCount > 0)
        {
            CurrentStacksCount--;
            _duration = _baseDuration * CurrentStacksCount;

            if (CurrentStacksCount == 0)
            {
                ExitState();
            }
        }
    }

    public override void ExitState()
    {
        if (_healthComponent != null)
        {
            _healthComponent.DamageTaken -= OnDamageTaken;
        }

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
            _duration += time;
            _duration = Mathf.Min(_duration, _baseDuration * CurrentStacksCount);
            var manaRestoreValue = _isTalentActive ? BuffedManaRestorePerStack : ManaRestorePerStack;
            ApplyManaRestore(manaRestoreValue * CurrentStacksCount);
        }

        return true;
    }

    private void ApplyManaRestore(float restoreValue)
    {
        if (_manaResource != null)
        {
            CmdApplyManaRestore(restoreValue, _characterState.Character.connectionToClient);
        }
    }

    private void OnDamageTaken(Damage damage, Skill skill)
    {
        if (_characterState?.Character == null) return;

        float manaRestoreValue = damage.Value * DamageManaRestorePercent * CurrentStacksCount;
        ApplyManaRestore(manaRestoreValue);
    }

    [Command]
    private void CmdApplyManaRestore(float restoreValue, NetworkConnectionToClient targetConnection)
    {
        TargetApplyManaRestore(targetConnection, restoreValue);
    }

    [TargetRpc]
    private void TargetApplyManaRestore(NetworkConnectionToClient target, float restoreValue)
    {
        if (_manaResource != null)
        {
            _manaResource.Add(restoreValue);
        }
    }
}
