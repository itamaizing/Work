using Org.BouncyCastle.Asn1.X509;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    private float _baseIncreaseAttackSpeed = 0.1f;
    private float _baseAttackSpeed;
    private float _increasedAttackSpeed = 1.0f;
    private float _maxMinimumAttackSpeed = 0.1f;

    private PoisonBoneState _poisonBoneState;
    private EmpathicPoisonsState _empathicPoisonState;
    private WitheringPoisonState _witheringPoisonState;
    private BindingPoisonState _bindingPoisonState;

    private void Start()
    {
        //Enter();
        _baseAttackSpeed = _creeperStrike.AttackSpeed;
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
                    var targetWithDebuff = target.GetComponent<CharacterState>();

                    if (targetWithDebuff.CheckPoisonStates())
                    {
                        AdvertisementStates(targetWithDebuff);

                        _enemiesWithDebuff.Add(target.gameObject);

                        if (_bindingPoisonState != null)
                        {
                            _currentStacksPoison += _bindingPoisonState.CurrentStacks;
                        }
                        if (_poisonBoneState != null)
                        {
                            _currentStacksPoison += _poisonBoneState.CurrentStacks;
                        }
                        if (_empathicPoisonState != null)
                        {
                            _currentStacksPoison += _empathicPoisonState.CurrentStacks; ;
                        }
                        if (_witheringPoisonState != null)
                        {
                            _currentStacksPoison += _witheringPoisonState.CurrentStacks;
                        }

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
                _increasedAttackSpeed = _baseAttackSpeed;
            }
            yield return null;
        }
    }

    private void IncreaseAttackSpeed()
    {
        if (_currentAllStacks > 0)
        {
            if (_increasedAttackSpeed > _maxMinimumAttackSpeed)
            {
                ResetAttackSpeed();
                _increasedAttackSpeed = _baseAttackSpeed - (_previousAllStacks * _baseIncreaseAttackSpeed);

                _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_increasedAttackSpeed);
            }
        }
    }

    private void ResetAttackSpeed()
    {
        if (_creeperStrike.AttackSpeed < _baseAttackSpeed)
        {
            float attackSpeed = _creeperStrike.AttackSpeed;

            _creeperStrike.Buff.AttackSpeed.ReductionPercentage(attackSpeed);
        }
    }

    private void AdvertisementStates(CharacterState targetWithDebuff)
    {
        _bindingPoisonState = (BindingPoisonState)targetWithDebuff.GetState(States.BindingPoison);
        _poisonBoneState = (PoisonBoneState)targetWithDebuff.GetState(States.PoisonBone);
        _empathicPoisonState = (EmpathicPoisonsState)targetWithDebuff.GetState(States.EmpathicPoisons);
        _witheringPoisonState = (WitheringPoisonState)targetWithDebuff.GetState(States.WitheringPoison);
    }
}
