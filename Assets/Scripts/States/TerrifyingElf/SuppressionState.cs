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

    private Resource manaResource;
    private Suppression _suppression;

    private float _baseDuration;
    private float _distBuffer;
    private bool _isMoving;

    private Vector3 _lastPosition; 

    private static readonly List<StatusEffect> _effects = new() { StatusEffect.Move };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Suppression;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
                                    Character caster, string skillName)
    {
        characterState = character;
        personWhoMadeBuff = caster;

        if (personWhoMadeBuff != null)
        {
            _suppression = personWhoMadeBuff.GetComponent<Suppression>();
        }

        _baseDuration = durationToExit;
        duration = _baseDuration;

        _distBuffer = 0f;
        _isMoving = false;

        _lastPosition = characterState.transform.position;
        _lastPosition.y = 0f;

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
        if (duration <= 0f)
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
        duration = _baseDuration;
        return true;
    }

    private void OnDamageTaken(Damage damage, Skill skill)
    {
        if (characterState.isServer) return;
        if (_suppression == null || !_suppression.IsSuppressionManaAbsorbtion) return;
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

        manaResource.CmdUse(burnAmount); 
    }

    #region Helpers
    private float CalcHorizontalDistanceThisFrame()
    {
        Vector3 currentPos = characterState.transform.position;
        currentPos.y = 0f;

        float dist = Vector3.Distance(currentPos, _lastPosition);
        _lastPosition = currentPos;
        return dist;
    }

    private void HandleVisuals(float deltaDist)
    {
        if (Time.deltaTime <= 0f) return;
        
        bool nowMoving = (deltaDist / Time.deltaTime) > MoveEpsilon;

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
        if (deltaDist <= 0f || manaResource == null) return;

        _distBuffer += deltaDist;

        int cells = Mathf.FloorToInt(_distBuffer / CellLength);
        if (cells <= 0) return;

        _distBuffer -= cells * CellLength;

        if (characterState.isServer)
        {
            float loss = cells * manaResource.MaxValue * ManaLossPerCell;
            manaResource.TryUse(loss);
        }
    }
    #endregion
}