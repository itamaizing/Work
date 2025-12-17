using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Teleportation_Scorpion : Skill /*, ICanConsumeComboPoints */
{
    [Header("Ability settings")]
    //[SerializeField] private VisualRender _visualRender;
    [SerializeField] private Character _playerLinks;
    [SerializeField] private DrawCircle _drawCircleSelf;
    [SerializeField] private int _baseManaCost;
    [SerializeField] private int _manaCostPerTile = 5;
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private float _offset = 0.5f;

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

    [SerializeField] private ConsumeCombo_Scorpion _consumeCombo_Scorpion;
    [SerializeField] private ScorpionPassive _scorpionPassive;

    [field: Header("Test Combo_Upgrade")]

    [field: SerializeField]
    public ConsumeCombo_Scorpion Notifier { get; set; }
    public int ConsumedAmount { get; set; }

    protected override bool IsCanCast
    {
        get
        {
            if (GetTargetCharacter() != null) return Vector3.Distance(GetTargetCharacter().transform.position, transform.position) <= Radius;

            var mana = _hero.Resources.FirstOrDefault(r => r.Type == ResourceType.Mana);
            if (mana == null) return false;

            if (GetTargetCharacter() != null)
            {
                float distance = Vector3.Distance(GetTargetCharacter().transform.position, transform.position);
                int manaCost = GetCurrentManaCost(distance);
                _skillEnergyCosts[0].resourceCost = manaCost;
                return distance <= Radius && mana.CurrentValue >= manaCost;

            }

            return mana.CurrentValue >= _baseManaCost;
        }
    }

    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == TargetsLayers;

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    //private void ResetValue()
    //{
    //    //IsCanCancle = true;
    //    _drawCircleSelf.Clear();
    //    //_target = null;
    //}

    //private bool IsMouseInRadius()
    //{
    //    float distance = Vector3.Distance(
    //        new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
    //        transform.position
    //        );

    //    return distance <= Radius;
    //}

    private Vector3 FindPlace(Character target)
    {
        Vector3 directionToEnemy = (target.transform.position - transform.position).normalized;

        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        float clampedDistance = Mathf.Min(distanceToTarget, Radius);

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

    //private int CalculateCurrentScale() // ��������� ���� ��� ����� ����������� ���������
    //{
    //    //_hero.Stamina.Value
    //    //_mana.value;
    //    if(_hero.Resources.First(o=>o.Type == ResourceType.Mana).CurrentValue >= _baseManaCost)
    //    {
    //        return (int)((_hero.Resources.First(o=>o.Type == ResourceType.Mana).CurrentValue - _baseManaCost) / 1);
    //    }

    //    return 0;
    //}

    private int GetCurrentManaCost(float distance)
    {
        int dist = Mathf.CeilToInt(distance);
        return _baseManaCost + dist * _manaCostPerTile;
    }
    //public void TryUpgradeByConsumingCombo(int amount)
    //{
    //    if (!Notifier.IsActive)
    //    {
    //        ConsumedAmount = 0;
    //        return;
    //    }

    //    ConsumedAmount =  Notifier.PayComboPoints(Mathf.Clamp(amount, 0, Notifier.AvailablePoints));

    //    // Change values
    //}

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        SetTarget((Character)targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (GetTempTargetCharacter() == null)
        {
            _drawCircleSelf.Draw(Radius);

            if (GetMouseButton)
            {
                FindTargetCharacter(SearchTargetInRadius, GetMousePoint());

                if (GetTempTargetCharacter() != null)
                {
                    if (IsAllyTarget(GetTempTargetCharacter()) || GetTempTargetCharacter() == Hero) ClearTarget();

                    else
                    {
                        float dist = Vector3.Distance(GetTempTargetCharacter().transform.position, transform.position);

                        if (dist > Radius)
                        {
                            Debug.Log("[Teleportation] Цель вне зоны действия");
                            continue;
                        }

                        int manaCost = GetCurrentManaCost(dist);
                        var mana = _hero.Resources.FirstOrDefault(r => r.Type == ResourceType.Mana);
                        if (mana == null || mana.CurrentValue < manaCost)
                        {
                            Debug.Log("[Teleportation] Недостаточно маны");
                            continue;
                        }

                        _skillEnergyCosts[0].resourceCost = manaCost;
                        break;
                    }
                }         
            }

            yield return null;
        }

        SetTargetCharacter(GetTempTargetCharacter());

        TargetInfo targetInfo = new();
        targetInfo.AddTarget(GetTargetCharacter());
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (GetTargetCharacter() == null) yield return null;

        float distance = Vector3.Distance(GetTargetCharacter().transform.position, transform.position);
        int manaToSpend = GetCurrentManaCost(distance);

        List<SkillEnergyCost> tempCosts = new()
        {
            new SkillEnergyCost
            {
                resourceType = _skillEnergyCosts[0].resourceType,
                resourceCost = manaToSpend
            }
        };

        if (!TryPayCost(tempCosts))
        {
            Debug.LogWarning("[Teleportation_Scorpion] Not enough mana!");
            yield break;
        }

        Vector3 tpPos = FindPlace(GetTargetCharacter());
        CmdTeleport(tpPos);

        int extraDuration = 0;
        var targetState = GetTargetCharacter().GetComponent<CharacterState>();

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
        ClearTarget();
    }

    [Command]
    private void CmdChangePosition(Vector3 teleportPosition)
    {
        _hero.transform.position = teleportPosition;
    }

    [Command]
    private void CmdTeleport(/*GameObject gameObject, */Vector3 newPosition)
    {
        //if (_tempTarget != gameObject)
        //{
        //    _tempTarget = gameObject;
        //    _tempTargetMove = gameObject.GetComponent<MoveComponent>();
        //}

        //_tempTargetMove.TargetRpcSetTransformPosition(newPosition);
        _hero.Move.TargetRpcSetTransformPosition(newPosition);
    }

    public void Teleportation_ScorpionMagResist(bool value)
    {
        isTeleportation_ScorpionMagResist = value;
    }
}
