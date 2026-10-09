using System.Collections.Generic;
using UnityEngine;

public class SuppressionState : StateBasic
{
    private const float CellLength = 0.1f;
    private const float ManaPercentPerMeter = 0.01f;
    private const float ManaStealFromDamage = 0.25f;
    private const float MoveEpsilon = 0.05f;

    private struct HitRecord
    {
        public float SumBefore;
        public bool Qualifies;
    }

    private static readonly List<StatusEffect> _effects = new() { StatusEffect.Move };
    private readonly List<HitRecord> _hits = new();

    private GameObject _suppressionIdle;
    private GameObject _suppressionMove;
    private Resource _targetMana;
    private Suppression _suppression;

    private Vector3 _lastPosition;
    private float _distBuffer;
    private bool _isMoving;
    private bool _isActive;

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Suppression;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character caster, string skillName)
    {
        _isActive = true;
        _suppression = sourceCaster != null ? sourceCaster.GetComponent<Suppression>() : null;
        _targetMana = characterState.Character.TryGetResource(ResourceType.Mana);

        _distBuffer = 0f;
        _isMoving = false;
        _lastPosition = Flat(characterState.transform.position);
        _hits.Clear();

        if (characterState.isServer)
        {
            health.OnBeforeDamage -= OnBeforeDamage;
            health.OnBeforeDamage += OnBeforeDamage;
        }

        _suppressionIdle = characterState.StateEffects.SuppressionIdle;
        _suppressionMove = characterState.StateEffects.SuppressionMove;
        if (_suppressionIdle) _suppressionIdle.SetActive(true);
        if (_suppressionMove) _suppressionMove.SetActive(false);
    }

    public override void Reapply(CharacterState character, float durationToExit, float damageToExit,
        Character caster, string skillName)
    {
        if (!_isActive)
        {
            Apply(character, durationToExit, damageToExit, caster, skillName);
            return;
        }
        _suppression = sourceCaster != null ? sourceCaster.GetComponent<Suppression>() : null;
    }

    public override void UpdateState()
    {
        if (characterState.isServer) ProcessHits();

        float deltaDist = CalcHorizontalDistanceThisFrame();
        HandleVisuals(deltaDist);
        DrainManaByDistance(deltaDist);
    }

    protected override void OnExit()
    {
        if (characterState.isServer) ProcessHits();

        _isActive = false;
        if (health != null) health.OnBeforeDamage -= OnBeforeDamage;
        if (_suppressionIdle) _suppressionIdle.SetActive(false);
        if (_suppressionMove) _suppressionMove.SetActive(false);

        base.ExitState();
    }
    
    private void OnBeforeDamage(ref Damage damage, Skill skill)
    {
        bool qualifies =
            _suppression != null && _suppression.IsSuppressionManaAbsorbtion
            && damage.Value > 0f
            && skill != null && skill.Hero != null
            && IsFromRequiredSource(skill.Hero);

        _hits.Add(new HitRecord { SumBefore = health.SumDamageTaken, Qualifies = qualifies });
    }

    private void ProcessHits()
    {
        if (_hits.Count == 0) return;

        float now = health.SumDamageTaken;
        for (int i = 0; i < _hits.Count; i++)
        {
            if (!_hits[i].Qualifies) continue;

            float end = i + 1 < _hits.Count ? _hits[i + 1].SumBefore : now;
            float dealt = end - _hits[i].SumBefore;
            if (dealt > 0f) StealMana(dealt * ManaStealFromDamage);
        }
        _hits.Clear();
    }

    private void StealMana(float amount)
    {
        if (_targetMana == null) return;

        amount = Mathf.Min(amount, _targetMana.CurrentValue);
        if (amount <= 0f) return;

        _targetMana.TryUse(amount);

        sourceCaster?.TryGetResource(ResourceType.Mana)?.Add(amount);
    }

    private bool IsFromRequiredSource(Character attacker)
        => attacker == sourceCaster || attacker.TryGetComponent<GhostAura>(out _);

    private void DrainManaByDistance(float deltaDist)
    {
        if (!characterState.isServer || deltaDist <= 0f || _targetMana == null) return;

        _distBuffer += deltaDist;
        int cells = Mathf.FloorToInt(_distBuffer / CellLength);
        if (cells <= 0) return;
        _distBuffer -= cells * CellLength;

        float loss = cells * CellLength * ManaPercentPerMeter * _targetMana.MaxValue;
        loss = Mathf.Min(loss, _targetMana.CurrentValue);
        if (loss > 0f) _targetMana.TryUse(loss);
    }

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    private float CalcHorizontalDistanceThisFrame()
    {
        Vector3 pos = Flat(characterState.transform.position);
        float dist = Vector3.Distance(pos, _lastPosition);
        _lastPosition = pos;
        return dist;
    }

    private void HandleVisuals(float deltaDist)
    {
        if (Time.deltaTime <= 0f) return;

        bool nowMoving = (deltaDist / Time.deltaTime) > MoveEpsilon;
        if (nowMoving == _isMoving) return;
        _isMoving = nowMoving;

        if (_suppressionIdle) _suppressionIdle.SetActive(!_isMoving);
        if (_suppressionMove) _suppressionMove.SetActive(_isMoving);
    }
}