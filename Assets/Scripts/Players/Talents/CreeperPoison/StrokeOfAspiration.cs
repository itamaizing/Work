using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrokesOfAspiration : MonoBehaviour
{
    [SerializeField] private Character _dad;
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private SpitPoison _spitPoison;
    [SerializeField] private CreeperStrike _creeperStrike;

    private float _maxHitCount = 2;
    private float _currentHitCount;

    private const float _timeBetweenAttack = 0.1f;
    private const float _decreaseCooldownTime = 0.3f;

    //private bool _isActive = false;

    private GameObject _currentTarget;
    private GameObject _lastTarget;

    private Coroutine _useTalentCoroutine;
    public Coroutine StartJobTalentCoroutine;

    private void Awake()
    {
        Debug.Log("Talent Awake Work");
        _currentHitCount = _maxHitCount;
        InitializationAbilities();
    }

    public IEnumerator StartJobTalent()
    {
        Debug.Log("Talent StartJobTalent Work");
        _currentHitCount--;
        UseTalent();
        yield return null;
    }

    private void UseTalent()
    {
        Debug.Log("Talent UseTalent Work");

        if (_creeperStrike.CurrentTarget != null)
        {
            _currentTarget = _creeperStrike.CurrentTarget;
            Debug.Log("CurrentTarget == " + _currentTarget);
            Debug.Log("CurrentHitCount == " + _currentHitCount);

            if (_currentHitCount <= 0 && _lastTarget == _currentTarget)
            {
                // Логика уменьшения времени перезарядки
                
            }
            else
            {
                _lastTarget = _currentTarget; 
                Debug.Log("LastTarget == " + _lastTarget);
            }
        }
        
        if (_currentHitCount == 0)
            _currentHitCount = _maxHitCount;
    }

    private void InitializationAbilities()
    {
        //_isActive = true;
        _poisonBall = _dad.GetComponentInChildren<PoisonBall>();
        _spitPoison = _dad.GetComponentInChildren<SpitPoison>();
        _creeperStrike = _dad.GetComponentInChildren<CreeperStrike>();
    }


}
