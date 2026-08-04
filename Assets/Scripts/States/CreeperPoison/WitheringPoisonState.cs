using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class WitheringPoisonState : RefreshingState
{
    private const int MaxPoisonStacks = 2;
    private const float TickInterval = 2f;
    private const float ResourceBurnPercent = 0.01f;

    private BindingPoison _bindingPoison;
    private Character _player;

    private float _tickTimer;
    private float _baseDuration;

    private float _baseChanceOfApplyBindingPoison = 0.03f;
    private float _chanceOfApplyBindingPoison = 0.9f;
    private bool _isActiveTalentBindingPoison = false;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Poison };

    public override States State => States.WitheringPoison;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = MaxPoisonStacks;
        _baseDuration = durationToExit;
        duration = durationToExit;
        _tickTimer = TickInterval;
        currentStacksCount = 1;

        _player = personWhoMadeBuff;

        if (_player != null)
        {
            var activeTalents = _player.CharacterState.Character.GetComponent<HeroComponent>().TalentManager.ActiveTalents;
            foreach (var talent in activeTalents)
            {
                if (talent is BindingPoison bindingPoison)
                {
                    _bindingPoison = bindingPoison;
                    _isActiveTalentBindingPoison = _bindingPoison.Data != null && _bindingPoison.Data.IsOpen;
                    break;
                }
            }
        }
    }

    public override void UpdateState()
    {
        _tickTimer -= Time.deltaTime;
        if (_tickTimer <= 0f)
        {
            if (characterState.isServer)
            {
                BurnMainResource();
            }
            _tickTimer = TickInterval;
        }

        if (currentStacksCount <= 0)
        {
            ExitState();
        }
    }

    public override bool Stack(float time)
    {
        duration = time;

        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
        }

        return true;
    }

    public override void ExitState()
    {
        currentStacksCount = 0;
        ResetValues();
        base.ExitState();
    }

    [Server]
    private void BurnMainResource()
    {
        if (characterState == null || characterState.Character == null) return;

        var mainResource = characterState.Character.Resource;
        if (mainResource != null && mainResource.CurrentValue > 0f)
        {
            float burnAmount = mainResource.CurrentValue * (ResourceBurnPercent * currentStacksCount);

            if (burnAmount > 0f)
            {
                mainResource.TryUse(burnAmount);
            }
        }

        if (_bindingPoison != null && _isActiveTalentBindingPoison)
        {
            _chanceOfApplyBindingPoison *= _baseChanceOfApplyBindingPoison;

            if (Random.value <= _chanceOfApplyBindingPoison)
            {
                characterState.AddState(States.BindingPoison, 10f, 0f, _player != null ? _player.gameObject : null, null);
            }
        }
    }

    private void ResetValues()
    {
        currentStacksCount = 0;
        _baseDuration = 0f;
        duration = 0f;
        _tickTimer = TickInterval;
        _chanceOfApplyBindingPoison = 0.9f;
        _bindingPoison = null;
        _player = null;
    }
    
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        else
            Stack(duration);

        return this;
    }
}