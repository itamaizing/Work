using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Obscurity : MonoBehaviour
{
    [SerializeField] private Toggle _toggleTalent;
    [SerializeField] private Toggle _toggleAbility;
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private GameObject _invisibleEffect;
    [SerializeField] private GameObject _cooldownButton;
    [SerializeField] private GameObject _panelInactive;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private GameObject _iconAbility;

    private float _distance = 1.94f * 5;
    private float _timerEvasion;
    private float _timerTakeDamage;
    private float _durationOfEvasion = 3f;
    private GameObject _player;
    private GameObject _playerAbility;
    private GameObject _newEffectPrefab;
    private bool _invisibleIsActive;
    private bool _evasion;
    private float _originalSpeed;
    private float _chanceToTurnOffInvisibility = 0.4f;
    private float _chanceOfEvasion = 0.1f;
    private float _persentageOfSpeedReduction = 0.3f;
    private float _impactCounter;
    private GameObject _target;
    private OneMeleeAttack _firstAbility;
    private TwoMeleeAttack _secondAbility;

    void Start()
    {
        _player = transform.parent.gameObject;
        _playerAbility = _player.transform.Find("Abilities").gameObject;
        _originalSpeed = _player.GetComponent<PlayerMove>().MoveSpeed;

        _firstAbility = _playerAbility.GetComponent<OneMeleeAttack>();
        _secondAbility = _playerAbility.GetComponent<TwoMeleeAttack>();

        _player.GetComponent<HealthPlayer>().OnTakeMagicDamage += TakeDamage;
        _player.GetComponent<HealthPlayer>().OnTakePhisicDamage += TakeDamage;

        _player.GetComponent<HealthPlayer>().OnTakePhisicDamage += EvadingPhysicalAttacks;

        _firstAbility.FirstAbilityEvent += damage => CheckClawsStrike();

        _playerAbility.GetComponent<CarrygunAbilitiesEvents>().CarrygunAbilitiesEvent += damage => StopInvisibility();

        _firstAbility.FirstAbilityEvent += damage => ImpositionOfBleeding(_firstAbility.Target, _firstAbility);
        _secondAbility.SecondAbilityEvent += damage => ImpositionOfBleeding(_secondAbility.Target, _secondAbility);
    }

    void Update()
    {
        if (_toggleTalent.isOn && !_toggleAbility.gameObject.activeSelf)
        {
            _toggleAbility.gameObject.SetActive(true);
        }
        else if(!_toggleTalent.isOn && _toggleAbility.gameObject.activeSelf)
        {
            _toggleAbility.isOn = false;
            _toggleAbility.gameObject.SetActive(false);
        }

        int activeChildCount = 0;
        foreach (Transform child in _toggleAbility.transform.parent)
        {
            if (child.gameObject.activeSelf)
            {
                activeChildCount++;

                if(child.gameObject == _toggleAbility.gameObject)
                {
                    break;
                }
            }
        }
        string key = "Alpha" + activeChildCount;

        if (Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), key)) && _toggleAbility.gameObject.activeSelf && _toggleAbility.transform.parent.gameObject.activeSelf)
        {
            if(_cooldownButton.gameObject.activeSelf)
            {
                _cooldownButton.GetComponent<Button>().onClick.Invoke();
            }
            else
            {
                if (_toggleAbility.enabled)
                {
                    _toggleAbility.isOn = !_toggleAbility.isOn;
                }
            }
        }

        if (!_toggleAbility.isOn && _invisibleIsActive)
        {
            StopInvisibility();
            _iconAbility.GetComponent<SpriteRenderer>().enabled = false;
        }

        if (_toggleAbility.gameObject.activeSelf && !_invisibleIsActive) 
        {
            if (CheckForEnemy() && !_panelInactive.activeSelf)
            {
                Debug.Log("Есть враг");
                _panelInactive.SetActive(true) ;
                _toggleAbility.enabled = false;
                return;
            }
            else if(!CheckForEnemy() && _panelInactive.activeSelf)
            {
                Debug.Log("Врагов нет");
                _panelInactive.SetActive(false);
                _toggleAbility.enabled = true;
            }

            if (_toggleAbility.isOn)
            {
                Debug.Log("активируем невидимость");

                _iconAbility.GetComponent<SpriteRenderer>().enabled = true;
                Color newColor = _iconAbility.GetComponent<SpriteRenderer>().color;
                newColor.a = 1f;
                _iconAbility.GetComponent<SpriteRenderer>().color = newColor;

                ActivateInvisibility();
                _invisibleIsActive = true;
            }
            
        }

        if(_timerTakeDamage > 0)
        {
            _timerTakeDamage -= Time.deltaTime;
            _cooldownButton.GetComponentInChildren<TextMeshPro>().text = ((int)_timerTakeDamage + 1).ToString();


            if (_timerTakeDamage <= 0)
            {
                if (!CheckForEnemy())
                {
                    _timerTakeDamage = 0;
                    _panelInactive.SetActive(false);
                    _toggleAbility.enabled = true;
                    _cooldownButton.SetActive(false);
                }
            }
        }
    }
    
    private bool CheckForEnemy()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _distance, _enemyLayer);

        foreach (Collider2D collider in colliders)
        {
            // Проверка наличия тега "Enemies"
            if (collider.CompareTag("Enemies"))
            {
                // Обнаружен враг
                return true;
            }
        }

        // Враг не обнаружен
        return false;
    }

    private void TakeDamage(HealthPlayer.DamageInfo damageInfo)
    {
        //Проверка нанесения урона

        if (!_invisibleIsActive)
        {
            _toggleAbility.enabled = false;
            _toggleAbility.isOn = false;

            _cooldownButton.SetActive(true);

            _timerTakeDamage = 3;
        }

        if (UnityEngine.Random.value <= _chanceToTurnOffInvisibility)
        {
            StopInvisibility();
        }
    }

    private void ActivateInvisibility()
    {
        _player.GetComponent<CharacterState>().ChangeState(new InvisibleState());

        _player.GetComponent<PlayerMove>().MoveSpeed = _originalSpeed - _originalSpeed * _persentageOfSpeedReduction;

        _newEffectPrefab = Instantiate(_invisibleEffect);
        _newEffectPrefab.transform.SetParent(_player.transform);

        for (int i = 0; i < _newEffectPrefab.transform.childCount; i++)
        {
            _newEffectPrefab.transform.GetChild(i).SetParent(_player.transform.Find("Effects"));
        }       
    }

    private void DisactivateInvisibility()
    {
        _player.GetComponent<PlayerMove>().MoveSpeed = _originalSpeed;
        if (_player.GetComponent<CharacterState>().CheckState() is InvisibleState)
        {
            _player.GetComponent<CharacterState>().ChangeState(new DefaultState());
        }

        Destroy(_newEffectPrefab);
        Destroy(_player.transform.Find("Effects").Find("ObscurityBaff").gameObject);
        _cooldownButton.gameObject.SetActive(false);
        _toggleAbility.isOn = false;
        _invisibleIsActive = false;
    }

    private void StopInvisibility()
    {
        if (_invisibleIsActive)
        {
            StartCoroutine(GetOutOfInvisibility());
            DisactivateInvisibility();
        }
    }


    //Выход  из незаметности
    private IEnumerator GetOutOfInvisibility()
    {
         _timerEvasion = _durationOfEvasion;
        _chanceOfEvasion = 0.1f;
        _evasion = true;

        while (_timerEvasion > 0)
        {
            yield return new WaitForSeconds(1);
            _timerEvasion --;
        }

        _evasion = false;
    }
    
    //Уклонение
    private void EvadingPhysicalAttacks(HealthPlayer.DamageInfo damageInfo)
    {
        if(_evasion)
        {
            if(UnityEngine.Random.value <= _chanceOfEvasion)
            {
                damageInfo.ModifiedDamage = 0;
            }
        }
    }

    //Если 2 удара когтями по одной цели, увеличиваем время и шанс уклонения
    private void CheckClawsStrike()
    {
        if (_evasion)
        {
            if (_impactCounter == 0)
            {
                _impactCounter++;
                _target = _firstAbility.Target;
            }
            else if(_impactCounter == 1)
            {
                GameObject newTarget = _firstAbility.Target;

                if (newTarget == _target)
                {
                    _impactCounter++;
                }
                else
                {
                    _target = _firstAbility.Target;
                }
            }

            if (_impactCounter == 2)
            {
                _timerEvasion = Mathf.Min(_timerEvasion + 3, 6);
                _chanceOfEvasion = Mathf.Min(_chanceOfEvasion + 0.1f, 0.4f);
            }
        }
    }

    //Наложение кровотечения по оглушенным целям
    private void ImpositionOfBleeding(GameObject target, AbilityBase ability)
    {
        if(_toggleTalent.isOn)
        {
            if(target.GetComponent<CharacterState>().CheckState() is StunnedState)
            {
                float bleedingChance = 0.9f;

                if(UnityEngine.Random.value < bleedingChance)
                {
                    _playerAbility.GetComponent<Bleeding>().StartBleed(target, ability);
                }
            }
        }
    }

    private void OnDisable()
    {
        _player.GetComponent<HealthPlayer>().OnTakeMagicDamage -= TakeDamage;
        _player.GetComponent<HealthPlayer>().OnTakePhisicDamage -= TakeDamage;

        _player.GetComponent<HealthPlayer>().OnTakePhisicDamage -= EvadingPhysicalAttacks;
    }
}
