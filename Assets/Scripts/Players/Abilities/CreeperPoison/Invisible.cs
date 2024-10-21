using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class Invisible : Ability
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private CharacterData _playerData;
    [SerializeField] private LayerMask _enemyLayerMask;
    [SerializeField] private LayerMask _obstacleLayerMask;
    //[SerializeField] private CircleCollider2D _searchingCircleCollider;
    private float timeWithoutDamage = 6.0f;

    private float increaseEnergyRegen = 0.3f;
    private float increaseEnergy;

    [SerializeField] private float reduceMoveSpeed = 0.3f;
    [SerializeField] private float moveSpeedDecrease;

    private float maxDistanceVisible = 12.0f / GlobalVariable.cellSize;

    [SerializeField] private bool _isUsing = false;

    private Coroutine _useJob;

    [SerializeField] private bool _enemyIsSees = false;
    private bool _enabled = false;
    private bool _isAttacked = false;

    private void Update()
    {
        if (!_enabled) return;
        //EnemyIsVisible();
        if (Input.GetMouseButtonDown(0))
            PayCost();
            Cast();

        if (Input.GetMouseButtonDown(1))
            Cancel();
    }
    protected override void Cast()
    {
        _enabled = true;
        Debug.Log("Cast() coroutine");
        _useJob = StartCoroutine(UseCoroutine());
    }

    protected override void Cancel()
    {
        _enabled = false;
        if (_useJob != null)
        {
            StopCoroutine(_useJob);
            ResetAbility();
        }
    }

    private IEnumerator UseCoroutine()
    {
        Collider2D hitEnemy = Physics2D.OverlapCircle(transform.position, maxDistanceVisible, _enemyLayerMask);
        Debug.Log("UseCoroutine hitEnemies = " + hitEnemy);
        if (hitEnemy == null)
        {
            Debug.Log("UseCoroutine. If < maxDistanceVisible. hit = " + hitEnemy);
            _enemyIsSees = false;
        }
        else if (hitEnemy != null)
        {
            Debug.Log("UseCoroutine. If > maxDistanceVisible. hit = " + hitEnemy);
            _enemyIsSees = true;
        }
        if (!_enemyIsSees && !_isUsing)
        {
            _isUsing = true;
            // ��������� �������� ������������ �� 30%
            moveSpeedDecrease = 1 - reduceMoveSpeed;
            _playerLinks.Move.ChangeMoveSpeed(moveSpeedDecrease);
            // ����������� ����� ������� �� 30%
            increaseEnergy = _playerLinks.Resources.FirstOrDefault()!.RegenerationValue * (1 + increaseEnergyRegen);
        }
        else if (_enemyIsSees && _isUsing)
        {
            ResetAbility();
        }
        else
        {
            yield return null;
        }
    }

    private void ResetAbility()
    {
        if (_isUsing)
        {
            // 1.1285715f - �����, ����� ������� �������� � ������������ ��������
            moveSpeedDecrease = 1.1285715f + reduceMoveSpeed;
            _playerLinks.Move.ChangeMoveSpeed(moveSpeedDecrease);
            // ��������� ����� ������� �� 30%
            increaseEnergy = _playerLinks.Resources.FirstOrDefault()!.RegenerationValue / (1 + increaseEnergyRegen);
            _isUsing = false;
        }
    }
}