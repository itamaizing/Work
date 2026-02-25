using Mirror;
using UnityEngine;

public class PsionicGenerationState : AbstractCharacterState
{
    private const float PsiPerTick = 10f;
    private const float TickInterval = 1f;

    private float _durationRemaining;
    private float _tickTimer;

    private BasePsionicEnergy _psionicEnergy;

    private readonly System.Collections.Generic.List<StatusEffect> _effects =
        new() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.PsionicGeneration;
    public override StateType Type => StateType.Magic;
    public override System.Collections.Generic.List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.personWhoMadeBuff = personWhoMadeBuff;

        _durationRemaining = durationToExit;

        if (!character.isServer) return;

        _psionicEnergy = character.GetComponent<BasePsionicEnergy>();
    }

    public override void UpdateState()
    {
        if (!characterState.Character.isServer) return;

        _durationRemaining -= Time.deltaTime;
        _tickTimer += Time.deltaTime;

        if (_tickTimer >= TickInterval)
        {
            _tickTimer = 0f;

            if (_psionicEnergy != null)
                _psionicEnergy.AddAndResetDecay(PsiPerTick);
        }

        if (_durationRemaining <= 0f)
            ExitState();
    }

    public override bool Stack(float time)
    {
        _durationRemaining = time;
        return false;
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);
    }
}