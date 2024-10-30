using Mirror;
using System.Collections;
using System.Linq;
using UnityEngine;

public class Teleportation_Scorpion : Skill, ICanConsumeComboPoints
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
    [SerializeField] private float _offset = 2.1f;
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

    protected override int AnimTriggerCastDelay => throw new System.NotImplementedException();

    protected override int AnimTriggerCast => throw new System.NotImplementedException();

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
        Vector3 directionToEnemy = target.transform.position - transform.position;
        directionToEnemy.Normalize();
        Vector3 offset = directionToEnemy * _offset;
        Vector3 teleportPosition = target.transform.position + offset;
        bool touchObstacle = Physics2D.OverlapCircle(teleportPosition, 2f, _layerMask);
        

        if (touchObstacle)
        {
            float angle = 0f;
            Vector3 newPosition;

            while (angle != 180) // ������ ����, �� ���� ����� � ��� �������
            {
                angle += 5;

                newPosition = _target.transform.position + Quaternion.Euler(0, 0, angle) * offset;
                if (!Physics2D.OverlapCircle(newPosition, _offset /2 , _layerMask))
                {
                    Debug.Log($"Place was found with offset angle {angle}�");
                    teleportPosition = newPosition;
                    return newPosition;
                }
                angle *= -1;
                newPosition = _target.transform.position + Quaternion.Euler(0, 0, angle) * offset;
                if (!Physics2D.OverlapCircle(newPosition, _offset / 2, _layerMask))
                {
                    Debug.Log($"Place was found with offset angle {angle}�");
                    teleportPosition = newPosition;
                    return newPosition;
                }
                angle *= -1;

            }
        }
        Debug.Log($"Place wasn't found or no obstacles at point, using default teleportPosition");
        return teleportPosition;
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

    public void TryUpgradeByConsumingCombo(int amount)
    {
        if (!Notifier.IsActive)
        {
            ConsumedAmount = 0;
            return;
        }

        ConsumedAmount =  Notifier.PayComboPoints(Mathf.Clamp(amount, 0, Notifier.AvailablePoints));

        // Change values
    }

    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
        {
            Radius = GetCurrentRadius();
            _drawCircleSelf.Draw(Radius);

            if (GetMouseButton)
            {
                _target = GetRaycastTarget(true);
                float dist = Vector2.Distance(_target.transform.position, transform.position);
                _skillEnergyCosts[0].resourceCost = GetCurrentManaCost(dist);
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        TryUpgradeByConsumingCombo(1);
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
