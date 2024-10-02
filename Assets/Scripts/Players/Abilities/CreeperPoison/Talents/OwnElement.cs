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

    private PoisonBoneState poisonBoneState;
    private EmpathicPoisonsState empathicPoisonState;
    private WitheringPoisonState witheringPoisonState;
    private BindingPoisonState bindingPoisonState;

    private void Start()
    {
        Enter();
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
        while (IsActive)
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

                        if (bindingPoisonState != null)
                        {
                            _currentStacksPoison += bindingPoisonState.CurrentStacks;
                        }
                        if (poisonBoneState != null)
                        {
                            _currentStacksPoison += poisonBoneState.CurrentStacks;
                        }
                        if (empathicPoisonState != null)
                        {
                            _currentStacksPoison += empathicPoisonState.CurrentStacks; ;
                        }
                        if (witheringPoisonState != null)
                        {
                            _currentStacksPoison += witheringPoisonState.CurrentStacks;
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
        bindingPoisonState = (BindingPoisonState)targetWithDebuff.GetState(States.BindingPoison);
        poisonBoneState = (PoisonBoneState)targetWithDebuff.GetState(States.PoisonBone);
        empathicPoisonState = (EmpathicPoisonsState)targetWithDebuff.GetState(States.EmpathicPoisons);
        witheringPoisonState = (WitheringPoisonState)targetWithDebuff.GetState(States.WitheringPoison);
    }
}
