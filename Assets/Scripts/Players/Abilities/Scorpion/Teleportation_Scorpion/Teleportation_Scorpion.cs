using Mirror;
using System.Collections;
using System.Linq;
using UnityEngine;

public class Teleportation_Scorpion : Skill /*, ICanConsumeComboPoints */
{
    [Header("Ability settings")]
    //[SerializeField] private VisualRender _visualRender;
    [SerializeField] private Character _playerLinks;
    [SerializeField] private DrawCircle _drawCircleSelf;
    [SerializeField] private float _minRadius;
    [SerializeField] private int _baseManaCost;
    [SerializeField] private int _manaCostPerTile = 5;
    [SerializeField] private LayerMask _layerMask;
    [Tooltip("��������� ������ ������ ���� ����������, ����� � ��������� �������")]
    [SerializeField] private float _offset = 0.5f;
    private Character _target;

    //private GameObject _tempTarget;
    //private MoveComponent _tempTargetMove;

    [SerializeField] private ConsumeCombo_Scorpion consumeCombo_Scorpion;

    [field: Header("Test Combo_Upgrade")]

    [field: SerializeField]
    public ConsumeCombo_Scorpion Notifier { get; set; }
    public int ConsumedAmount { get; set; }

    protected override bool IsCanCast
    {
        get
        {
            if (_target != null)
                return Vector3.Distance(_target.transform.position, transform.position) <= Radius;

            return false;
        }
    }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    protected void Start()
    {
        _minRadius = Radius;
    }

    private void ResetValue()
    {
        //IsCanCancle = true;
        _drawCircleSelf.Clear();
        _target = null;
    }

    private bool IsMouseInRadius()
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= GetCurrentRadius() /*Radius*/;
    }

    private Vector3 FindPlace(Character target)
    {
        Vector3 directionToEnemy = (target.transform.position - transform.position).normalized;

        Vector3 initialOffset = directionToEnemy * _offset;
        Vector3 teleportPosition = target.transform.position + initialOffset;

        if (!IsPositionBlocked(teleportPosition, _offset, target))
            return teleportPosition;

        float searchRadius = 1.5f;
        float angleStep = 10f;
        float maxAngle = 180f;
        Vector3 foundPoint = Vector3.zero;
        bool freePointFound = false;

        for (float angle = angleStep; angle <= maxAngle; angle += angleStep)
        {
            Quaternion rotationCW = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 offsetCW = rotationCW * directionToEnemy * searchRadius;
            Vector3 candidateCW = target.transform.position + offsetCW;

            if (!IsPositionBlocked(candidateCW, _offset, target))
            {
                foundPoint = candidateCW;
                freePointFound = true;
                break;
            }

            Quaternion rotationCCW = Quaternion.AngleAxis(-angle, Vector3.up);
            Vector3 offsetCCW = rotationCCW * directionToEnemy * searchRadius;
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

    private float GetCurrentRadius()
    {
        return _minRadius + 1f * (int)(CalculateCurrentScale() / _manaCostPerTile); // ����������� r + 1 ������ �� 5 ���� (������ �� 0.2 ������ �� 1 ����, ���� ���� ����� ���������, ������ ��������)
    }
    private int CalculateCurrentScale() // ��������� ���� ��� ����� ����������� ���������
    {
        //_hero.Stamina.Value
        //_mana.value;
        if(_hero.Resources.First(o=>o.Type == ResourceType.Mana || o.Type == ResourceType.Energy).CurrentValue >= _baseManaCost)
        {
            return (int)((_hero.Resources.First(o=>o.Type == ResourceType.Mana || o.Type == ResourceType.Energy).CurrentValue - _baseManaCost) / 1);
        }

        return 0;
    }
    private int GetCurrentManaCost(float distance)
    {
        int dist = (int)Mathf.Ceil(distance);

        if (dist <= 2) return _baseManaCost;
        else return (int)Mathf.Clamp(_baseManaCost + (dist - 2) * _manaCostPerTile, 0, _playerLinks.Resources.First(o=>o.Type == ResourceType.Mana || o.Type == ResourceType.Energy).MaxValue);

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

    protected override IEnumerator PrepareJob()
    {
        while (true)
        {
            Radius = GetCurrentRadius();
            _drawCircleSelf.Draw(Radius);

            if (GetMouseButton)
            {
                _target = GetRaycastTarget(true);

                if (_target == null)
                {
                    yield return null;
                    continue;
                }

                float dist = Vector3.Distance(_target.transform.position, transform.position);
                _skillEnergyCosts[0].resourceCost = GetCurrentManaCost(dist);
                break;
            }

            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        //TryUpgradeByConsumingCombo(1);
        Vector3 tpPos = FindPlace(_target);

        //CmdChangePosition(tpPos);
        CmdTeleport(tpPos);

        //CmdApplyBuff(_health.transform);

        _hero.CharacterState.CmdAddState(States.IdealEvade, 1f + ConsumedAmount, 30f, _hero.gameObject, name);

        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
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
}
