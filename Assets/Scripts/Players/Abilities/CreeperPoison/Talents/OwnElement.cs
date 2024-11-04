using Org.BouncyCastle.Asn1.X509;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class OwnElement : Talent
{
    //[SerializeField] private Test_AttackSpeedChangedSystem _attackSpeedChangedSystem;
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
    private float _increasedAttackSpeed;
    private float _newAttackSpeed = 1.0f;
    private float _maxMinimumAttackSpeed = 0.34f;

    private bool _isCanResetAttackSpeed = false;
    private bool _isTargetNearby = false;

    private PoisonBoneState _poisonBoneState;
    private EmpathicPoisonsState _empathicPoisonState;
    private WitheringPoisonState _witheringPoisonState;
    private BindingPoisonState _bindingPoisonState;

    private Coroutine _searchingDebuffOnEnemeies;

    private void Start()
    {
        _baseAttackSpeed = _creeperStrike.AttackSpeed;
    }

    public override void Enter()
    {
        SetActive(true);
        StartSearchingEnemies();
    }

    public override void Exit()
    {
        if (_searchingDebuffOnEnemeies != null)
        {
            StopCoroutine(_searchingDebuffOnEnemeies);
            _searchingDebuffOnEnemeies = null;
        }

        SetActive(false);
    }

    private void StartSearchingEnemies()
    {
        _searchingDebuffOnEnemeies = StartCoroutine(SearchingDebuffOnEnemy());
    }

    private IEnumerator SearchingDebuffOnEnemy()
    {
        while (Data.IsOpen)
        {
            _enemiesWithDebuff.Clear();
            _currentPoisonOnEnemy = 0;
            _currentStacksPoison = 0;
            _currentAllStacks = 0;
            _isTargetNearby = false;

            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, _radiusSearching, _enemyLayer);
            
            if (enemies != null)
            {
                foreach (Collider2D target in enemies)
                {
                    _isTargetNearby = true;

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
            if (_currentAllStacks == 0 || !_isTargetNearby)
            {
                if (_creeperStrike.AttackSpeed < _baseAttackSpeed)
                {
                    ResetAttackSpeed();
                }
            }
            yield return null;
        }
    }

    private void IncreaseAttackSpeed()
    {
        if (_currentAllStacks > 0)
        {
            if (_newAttackSpeed > _maxMinimumAttackSpeed) 
            { 
                Debug.Log("OwnElement / IncreaseAttackSpeed / Before Increased CreeperStrike AttackSpeed = " + _creeperStrike.AttackSpeed);
                _increasedAttackSpeed = _baseAttackSpeed - (_currentAllStacks * _baseIncreaseAttackSpeed);
                Debug.Log("OwnElement / IncreaseAttackSpeed / Before Increased _increasedAttackSpeed = " + _increasedAttackSpeed);
                _newAttackSpeed *= _increasedAttackSpeed;
                Debug.Log("OwnElement / IncreaseAttackSpeed / Before Increased _newAttackSpeed = " + _newAttackSpeed);

                if (_creeperStrike.AttackSpeed > _maxMinimumAttackSpeed)
                {
                    _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_newAttackSpeed);
                    Debug.Log("OwnElement / IncreaseAttackSpeed / After Increased CreeperStrike AttackSpeed = " + _creeperStrike.AttackSpeed);
                }

            }
        }
    }

    private void ResetAttackSpeed()
    {
        Debug.Log("OwnElement / ResetAttackSpeed / Before Reset CreeperStrike AttackSpeed = " + _creeperStrike.AttackSpeed);
        Debug.Log("OwnElement / ResetAttackSpeed / Before Reset _newAttackSpeed = " + _newAttackSpeed);
        Debug.Log("OwnElement / ResetAttackSpeed / Before Reset _increasedAttackSpeed = " + _increasedAttackSpeed);

        if (_creeperStrike.AttackSpeed < _baseAttackSpeed && _newAttackSpeed < 0.99f)
        {
            _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_newAttackSpeed);
            _newAttackSpeed /= _increasedAttackSpeed;
            _increasedAttackSpeed += 0.1f;

            Debug.Log("OwnElement / ResetAttackSpeed / After Reset _newAttackSpeed = " + _newAttackSpeed);
            Debug.Log("OwnElement / ResetAttackSpeed / After Reset _increasedAttackSpeed = " + _increasedAttackSpeed);
            Debug.Log("OwnElement / ResetAttackSpeed / After Reset CreeperStrike AttackSpeed = " + _creeperStrike.AttackSpeed);
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
