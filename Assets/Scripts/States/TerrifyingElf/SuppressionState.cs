using System.Collections.Generic;
using UnityEngine;

public class SuppressionState : AbstractCharacterState
{
    private const int MaxStacks = 1;

    private const float CellLength = 0.10f;
    private const float ManaLossPerCell = 0.001f;
    private const float MoveEpsilon = 0.05f;

    private GameObject _suppressionIdle;
    private GameObject _suppressionMove;

    private MoveComponent _move;
    private Rigidbody _rigidbody;
    private Resource manaResource;
    private Suppression _suppression;

    private float _baseDuration;
    private float _duration;
    private int _currentStacks = 1;

    private float _distBuffer;
    private bool _isMoving;

    private static readonly List<StatusEffect> _effects = new() { StatusEffect.Move };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Suppression;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit,
                                    Character caster, string skillName)
    {
        characterState = character;
        personWhoMadeBuff = caster;

        _suppression = personWhoMadeBuff.GetComponent<Suppression>();

        _baseDuration = durationToExit;
        _duration = _baseDuration;

        _move = character.Character.GetComponent<MoveComponent>();
        _rigidbody = _move != null ? _move.Rigidbody : character.Character.GetComponent<Rigidbody>();

        _distBuffer = 0f;
        _isMoving = false;

        manaResource = character.Character.TryGetResource(ResourceType.Mana);

        health = character.Character.Health;
        health.DamageTaken += OnDamageTaken;

        _suppressionIdle = characterState.StateEffects.SuppressionIdle;
        _suppressionMove = characterState.StateEffects.SuppressionMove;

        if (_suppressionIdle) _suppressionIdle.SetActive(true);
        if (_suppressionMove) _suppressionMove.SetActive(false);
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0f)
        {
            ExitState();
            return;
        }

        float deltaDist = CalcHorizontalDistanceThisFrame();
        HandleVisuals(deltaDist);
        DrainManaByDistance(deltaDist);
    }

    public override void ExitState()
    {
        if (_suppressionIdle) _suppressionIdle.SetActive(false);
        if (_suppressionMove) _suppressionMove.SetActive(false);

        characterState.StateIcons.RemoveItemByState(State);
        characterState.RemoveState(this);

        if (health != null) health.DamageTaken -= OnDamageTaken;
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < MaxStacks) _currentStacks++;
        _duration = _baseDuration;
        return true;
    }

    private void OnDamageTaken(Damage damage, Skill skill)
    {
        if (!characterState.isServer) return;
        if (!_suppression.IsSuppressionManaAbsorbtion) return;
        if (skill == null || skill.Hero == null) return;

        Character attacker = skill.Hero;

        if (!IsFromRequiredSource(attacker)) return;

        ApplyManaBurn(damage.Value);
    }

    private bool IsFromRequiredSource(Character attacker)
    {
        if (attacker == null) return false;

        if (attacker.TryGetComponent<TerrifyingElfAura>(out _)) return true;
        if (attacker.TryGetComponent<GhostAura>(out _)) return true;

        return false;
    }

    private void ApplyManaBurn(float damageValue)
    {
        if (manaResource == null) return;

        float burnAmount = damageValue * 0.25f;

        float currentMana = manaResource.CurrentValue;
        float newMana = Mathf.Max(0, currentMana - burnAmount);

        manaResource.TryUse(newMana);
    }

    #region Helpers
    private float CalcHorizontalDistanceThisFrame()
    {
        if (_rigidbody == null) return 0f;

        Vector3 distance = _rigidbody.linearVelocity;
        distance.y = 0f;
        return distance.magnitude * Time.deltaTime;
    }

    private void HandleVisuals(float deltaDist)
    {
        bool nowMoving = deltaDist / Time.deltaTime > MoveEpsilon;

        if (nowMoving == _isMoving) return;

        _isMoving = nowMoving;

        if (_isMoving)
        {
            if (_suppressionIdle) _suppressionIdle.SetActive(false);
            if (_suppressionMove) _suppressionMove.SetActive(true);
        }
        else
        {
            if (_suppressionMove) _suppressionMove.SetActive(false);
            if (_suppressionIdle) _suppressionIdle.SetActive(true);
        }
    }

    private void DrainManaByDistance(float deltaDist)
    {
        if (deltaDist <= 0f) return;

        _distBuffer += deltaDist;

        int cells = Mathf.FloorToInt(_distBuffer / CellLength);
        if (cells <= 0) return;

        _distBuffer -= cells * CellLength;

        if (characterState.Character.TryGetResource(ResourceType.Mana) is Mana mana)
        {
            float loss = cells * mana.MaxValue * ManaLossPerCell;
            mana.TryUse(loss);
        }
    }
    #endregion
}
