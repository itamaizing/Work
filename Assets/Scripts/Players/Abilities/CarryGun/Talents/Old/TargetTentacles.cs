using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetTentacles : MonoBehaviour
{
    [SerializeField] private Toggle _toggleTalent;

    private GameObject _player;
    private GameObject _playerAbility;
    private GameObject _target;

    private FourMeleeAttack _tentaclesAttack;
    private TwoMeleeAttack _heliceraAbility;
    private OneMeleeAttack _handDamageAbility;

    private Coroutine _moveCoroutine;
    private bool _isUpgradeHandDamage;
    private bool _isUpgradeHelicera;
    private float _timer = 0f;
    private float _originalDamageRate;
    private float _countImpact;
    private float _distance = 1.94f * 2f;
    private float _moveSpeed = 1.94f / 0.3f;
    private float _activePsionica;

    void Start()
    {
        _player = transform.parent.gameObject;
        _playerAbility = _player.transform.Find("Abilities").gameObject;

        _tentaclesAttack = _playerAbility.GetComponent<FourMeleeAttack>();
        _heliceraAbility = _playerAbility.GetComponent<TwoMeleeAttack>();
        _handDamageAbility = _playerAbility.GetComponent<OneMeleeAttack>();

        _heliceraAbility.SecondAbilityEvent += damage => JumpAfterHeliceraAttack();
        _handDamageAbility.FirstAbilityEvent += damage => ImpactCounter();
        _tentaclesAttack.FourthAbilityEvent += damage => UpgradeAbilities(_tentaclesAttack.Target);

    }

    private void Update()
    {
        _activePsionica = _playerAbility.GetComponent<FiveConversion>().PsionicaActive;

        if (_isUpgradeHandDamage)
        {
            _timer -= Time.deltaTime;

            // Если прошла 1 секунда, сбрасываем флаг и возвращаем исходное значение
            if (_timer <= 0f)
            {
                _handDamageAbility.DamageRate = _originalDamageRate;
                _handDamageAbility.TargetCanAvoidance = true;
                _handDamageAbility.IsGlobalCooldown = true;

                _isUpgradeHandDamage = false;
                _isUpgradeHelicera = false;
            }

            if(_countImpact >= 2)
            {
                _handDamageAbility.DamageRate = _originalDamageRate;
                _handDamageAbility.TargetCanAvoidance = true;
                _handDamageAbility.IsGlobalCooldown = true;

                if(_moveCoroutine != null)
                {
                    _tentaclesAttack.AbilityCooldownTime /= 2;
                }
                _isUpgradeHandDamage = false;
            }
        }
    }

    private void UpgradeAbilities(GameObject gameObject)
    {
        //Успешное применение «Щупальца» 
        if (_toggleTalent.isOn)
        {
            _target = gameObject;
            _isUpgradeHandDamage = true;
            _isUpgradeHelicera = true;
            _timer = 1f;
            _countImpact = 0;
            _moveCoroutine = null;

            _originalDamageRate = _handDamageAbility.DamageRate;

            if (_handDamageAbility.Target == _target)
            {
                _handDamageAbility.DamageRate /= 5f;
                _handDamageAbility.IsGlobalCooldown = false;
            }
        }

    }

    private void ImpactCounter()
    {
        if( _isUpgradeHandDamage )
        {
            _countImpact += 1;

            if (_countImpact < 2 && _activePsionica >= 10 && _activePsionica < 20)
            {
                _handDamageAbility.DamageRate = 0;
                _handDamageAbility.TargetCanAvoidance = false;
            }
        }
    }

    private void JumpAfterHeliceraAttack()
    {
        if(_isUpgradeHelicera && _heliceraAbility.Target == _target)
        {
            Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            cursorPosition.z = _player.transform.position.z; // Устанавливаем z-координату курсора равной z-координате игрока

            if(_activePsionica >= 20 && _activePsionica < 30)
            {
                _distance = 1.94f * 3;
            }
            else if(_activePsionica >= 30)
            {
                _distance = 1.94f * 4;
            }

            Vector3 direction = (cursorPosition - _player.transform.position).normalized;
            Vector3 targetPosition = _player.transform.position + direction * _distance; // Целевая позиция для перемещения

            // Плавное перемещение игрока к курсору
            if (_activePsionica < 10)
            {
                StartCoroutine(MoveTowardsCoroutine(_player, targetPosition, _moveSpeed));
            }
            else 
            {
                _player.transform.position = targetPosition;
            }
        }
    }

    private IEnumerator MoveTowardsCoroutine(GameObject target, Vector3 targetPosition, float moveSpeed)
    {
        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();

        while (target.transform.position != targetPosition && _isUpgradeHelicera)
        {
            rb.isKinematic = false;
            rb.MovePosition(Vector2.MoveTowards(target.transform.position, targetPosition, moveSpeed * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }

        rb.isKinematic = true;
    }


    private void OnDisable()
    {
        _tentaclesAttack.FourthAbilityEvent -= damage => UpgradeAbilities(_tentaclesAttack.Target);
        _handDamageAbility.FirstAbilityEvent -= damage => ImpactCounter();

    }
}
