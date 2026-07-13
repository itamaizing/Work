using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Teleportation_Scorpion : Skill /*, ICanConsumeComboPoints */
{
    [Header("Ability settings")]
    [SerializeField] private int _manaCostPerTile = 5;
    [SerializeField] private int _baseCost = 20;
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private float _offset = 0.5f;

    private Resource _energy;
    private Attribute _radiusAttribute;
    AttributeModifier _bonusCostModifier = new AttributeModifier(0, ModifierType.Flat);
    private AttributeModifier _radiusModifierAttribute = new(0, ModifierType.Flat);
    private bool isTeleportation_ScorpionMagResist;

    #region Const
    private const float MaxSearchAngle = 180f;
    private const float SearchAngleStep = 10f;
    private const float SearchRadius = 1.5f;
    private const float IdealEvadeChance = 0.3f;
    private const float IdealEvadeDurationBase = 1f;
    private const float IdealEvadePower = 30f;
    private const float SearchTargetInRadius = 1f;
    #endregion

    #region CostDiscountTalent

    private bool _isScorchedSoulDiscount;

    public void EnableScorchedSoulDiscount(bool value)
    {
        if(value == _isScorchedSoulDiscount) return;
        
        _isScorchedSoulDiscount = value;
    }
    
    private float GetScorchedSoulDivisor(Character target)
    {
        if (!_isScorchedSoulDiscount) return 1f;
        if (target == null) return 1f;

        int stacks = target.CharacterState.CheckStateStacks(States.ScorchedSoul);
        return stacks > 0 ? stacks + 1f : 1f;
    }

    #endregion
    
    [SerializeField] private ScorpionPassive _scorpionPassive;
    

    [field: Header("Test Combo_Upgrade")]

    [field: SerializeField]
    public ConsumeCombo_Scorpion Notifier { get; set; }

    protected override bool IsCanCast
    {
        get
        {
            var energy = _hero.Resources[ResourceType.Energy];
            if (energy == null) return false;

            if (Targeting.GetTarget()?.Character != null)
            {
                float distance = Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position);
                int bonusCost = GetBonusCost(distance);
                return energy.CurrentValue >= (_baseCost+bonusCost) / GetScorchedSoulDivisor(Targeting.GetTarget().Character);
            }
            return false;
        }
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render,hero);
        _energy = _hero.Resources[ResourceType.Energy];
        _radiusAttribute = Attributes[SkillAttributeName.Radius];
        PreparingStarted += SetRadius;
    }

    private void OnDestroy()
    {
        PreparingStarted -= SetRadius;
    }

    private void SetRadius(Skill skill)
    {
        _radiusAttribute.RemoveModifier(_radiusModifierAttribute);
        var newRadius = (_energy.CurrentValue - _baseCost) / _manaCostPerTile;
        _radiusModifierAttribute.Value = newRadius;
        _radiusAttribute.AddModifier(_radiusModifierAttribute);
    }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private Vector3 FindPlace(Character target)
    {
        Vector3 directionToEnemy = (target.transform.position - transform.position).normalized;

        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        float clampedDistance = Mathf.Min(distanceToTarget, AreaInfo.Radius);

        Vector3 teleportBasePosition = transform.position + directionToEnemy * clampedDistance;
        Vector3 initialOffset = directionToEnemy * _offset;
        Vector3 teleportPosition = teleportBasePosition + initialOffset;

        if (!IsPositionBlocked(teleportPosition, _offset, target))
            return teleportPosition;

        Vector3 foundPoint = Vector3.zero;
        bool freePointFound = false;

        for (float angle = SearchAngleStep; angle <= MaxSearchAngle; angle += SearchAngleStep)
        {
            Quaternion rotationCW = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 offsetCW = rotationCW * directionToEnemy * SearchRadius;
            Vector3 candidateCW = target.transform.position + offsetCW;

            if (!IsPositionBlocked(candidateCW, _offset, target))
            {
                foundPoint = candidateCW;
                freePointFound = true;
                break;
            }

            Quaternion rotationCCW = Quaternion.AngleAxis(-angle, Vector3.up);
            Vector3 offsetCCW = rotationCCW * directionToEnemy * SearchRadius;
            Vector3 candidateCCW = target.transform.position + offsetCCW;

            if (!IsPositionBlocked(candidateCCW, _offset, target))
            {
                foundPoint = candidateCCW;
                freePointFound = true;
                break;
            }
        }

        if (freePointFound)
        {
            Vector3 dirToTarget = (target.transform.position - foundPoint).normalized;
            Vector3 closeToTarget = target.transform.position - dirToTarget * _offset;

            if (!IsPositionBlocked(closeToTarget, _offset, target))
                return closeToTarget;

            return foundPoint;
        }

        return transform.position;
    }

    private bool IsPositionBlocked(Vector3 position, float radius, Character targetToIgnore)
    {
        Collider[] colliders = Physics.OverlapSphere(position, radius, _layerMask);

        foreach (var collider in colliders)
        {
            if (collider.transform == targetToIgnore.transform) continue;

            return true;
        }

        return false;
    }

    private int GetBonusCost(float distance)
    {
        int dist = Mathf.CeilToInt(distance);
        int bonusCost = dist * _manaCostPerTile;

        return bonusCost;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((Character)targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), SearchTargetInRadius);

                if (Targeting.GetTempTarget()?.Character != null)
                {
                    if (Targeting.GetTempTarget()?.Character == Hero) Targeting.ClearTempTarget();

                    else
                    {
                        float dist = Vector3.Distance(Targeting.GetTempTarget().Character.transform.position, transform.position);

                        if (dist > AreaInfo.Radius)
                        {
                            continue;
                        }

                        break;
                    }
                }
            }

            yield return null;
        }
       
        float distance = Vector3.Distance(Targeting.GetTempTarget().Character.transform.position, transform.position);
        float divisor = GetScorchedSoulDivisor(Targeting.GetTempTarget().Character);
        int totalCost  = GetBonusCost(distance) + _baseCost;
        Debug.LogError("total cost: " + totalCost);
        int discounted = Mathf.RoundToInt(totalCost / divisor);
        Debug.LogError("discounted: " + discounted);

        _bonusCostModifier.Value = discounted - _baseCost;
        Attributes[SkillAttributeName.ResourceCost].AddModifier(_bonusCostModifier);

        TargetInfo targetInfo = new();
        targetInfo.AddTarget(Targeting.GetTempTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character == null) yield return null;

        Vector3 tpPos = FindPlace(Targeting.GetTarget()?.Character);
        CmdTeleport(tpPos);

        int extraDuration = 0;
        var targetState = Targeting.GetTarget()?.Character.GetComponent<CharacterState>();

        if (isTeleportation_ScorpionMagResist)
        {
            if (targetState != null) extraDuration = targetState.CheckStateStacks(States.ComboState);
            if (UnityEngine.Random.value <= IdealEvadeChance) _hero.CharacterState.CmdAddState(States.IdealEvade, IdealEvadeDurationBase + extraDuration, IdealEvadePower, _hero.gameObject, name);
        }

        if (_scorpionPassive.IsImpulseMatter)
        {
            var passive = _hero.GetComponent<SkillManager>().Abilities.FirstOrDefault(s => s is ScorpionPassive) as ScorpionPassive;

            passive?.ActivateEnergyFreeAfterTeleport();
        }
        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        Attributes[SkillAttributeName.ResourceCost].RemoveModifier(_bonusCostModifier);
    }

    [Command]
    private void CmdTeleport(Vector3 newPosition)
    {
        _hero.Move.TargetRpcSetTransformPosition(newPosition);
    }

    public void Teleportation_ScorpionMagResist(bool value)
    {
        isTeleportation_ScorpionMagResist = value;
    }
}
