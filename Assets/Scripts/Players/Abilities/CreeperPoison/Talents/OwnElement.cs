using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OwnElement : Talent
{
    [SerializeField] private CreeperStrike _creeperStrike;

    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private List<GameObject> _enemiesWithDebuff = new();

    [SerializeField] private float _radiusSearching;

    private int _currentPoisonOnEnemy;
    private int _currentStacksPoison;
    private int _currentAllStacks;
    private int _previousAllStacks;

    private float _increaseAttackSpeed = 0.1f;
    private float _baseAttackSpeed;
    private float _increasedAttackSpeedCreeperStrike = 1.0f;


    private void Start()
    {
        _baseAttackSpeed = _creeperStrike.AttackSpeed;
        Debug.Log("Start / _baseAttackSpeed == " + _baseAttackSpeed);
    }

    public override void Enter()
    {
        SetActive(true);
        StartCoroutine(SearchingDebuffOnEnemy());
    }

    public override void Exit()
    {
        StopCoroutine(SearchingDebuffOnEnemy());
        SetActive(false);
    }

    private IEnumerator SearchingDebuffOnEnemy()
    {
        while (Data.IsOpen)
        {
            _enemiesWithDebuff.Clear();
            _currentPoisonOnEnemy = 0;
            _currentStacksPoison = 0;
            _currentAllStacks = 0;

            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, _radiusSearching, _enemyLayer);
            if (enemies != null)
            {
                foreach (Collider2D target in enemies)
                {
                    var targetWithDebuffPoisonBone = target.GetComponent<CharacterState>().CheckForState(States.PoisonBone);
                    Debug.Log($"OwnElement / SearchingDebuffEnemy / targetWithDebuff = {target.GetComponent<CharacterState>().CheckForState(States.PoisonBone)}");

                    if (targetWithDebuffPoisonBone != false)
                    {
                        _enemiesWithDebuff.Add(target.gameObject);

                        //_currentStacksPoison += PoisonBone.CurrentStacks;
                        Debug.Log($"OwnElement / SearchingDebuffEnemy / _currentStacksPoison = {_currentStacksPoison}");
                        for (int i = 0; i < _enemiesWithDebuff.Count; i++)
                        {
                            _currentPoisonOnEnemy = _enemiesWithDebuff.Count;
                        }
                    }
                }

                _currentAllStacks = _currentPoisonOnEnemy + _currentStacksPoison;

                if (_currentAllStacks != _previousAllStacks)
                {
                    IncreaseAttackSpeed();
                    _previousAllStacks = _currentAllStacks;
                }
            }
            if (_currentAllStacks == 0)
            {
                if (_creeperStrike.AttackSpeed != _baseAttackSpeed)
                ResetAttackSpeed();
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void IncreaseAttackSpeed()
    {
        if (_currentAllStacks > 0)
        {
            if (_increasedAttackSpeedCreeperStrike > 0.01f)
            {
                ResetAttackSpeed();
                _increasedAttackSpeedCreeperStrike = _baseAttackSpeed - (_previousAllStacks * _increaseAttackSpeed);
                Debug.Log("OwnElement / Increased attack speed == " + _increasedAttackSpeedCreeperStrike);

                _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_increasedAttackSpeedCreeperStrike);
                Debug.Log("OwnElement / _Creeper attack speed == " + _creeperStrike.AttackSpeed);
            }
        }
    }

    private void ResetAttackSpeed()
    {
        if (_increasedAttackSpeedCreeperStrike < 1.0f)
        {
            Debug.Log("OwnElement / Reset Increased attack speed == " + _increasedAttackSpeedCreeperStrike);
            _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_increasedAttackSpeedCreeperStrike);
            Debug.Log("OwnElement / ResetAttackSpeed _Creeper attack speed == " + _creeperStrike.AttackSpeed);
        }
    }
}
