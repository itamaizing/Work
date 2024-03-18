using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PoisonTalent : MonoBehaviour
{
    [SerializeField] private Toggle _toggleTalent;
    [SerializeField] private GameObject _poisonDebuff;
    [SerializeField] private GameObject _poisonIcon;
    [SerializeField] private TextMeshPro _textPoison;
    [SerializeField] private TextMeshPro _textCooldown;
    public float PsionicaValue;

    private GameObject _player;
    private GameObject _playerAbility;
    private TwoMeleeAttack _heliceraAbility;
    private OneMeleeAttack _handDamageAbility;
    private float _timer = 0f;
    private float _poisonChance = 0.5f;
    private GameObject _newPrefab;

    public int Poison;


    private void Start()
    {
        _player = transform.parent.gameObject;
        _playerAbility = _player.transform.Find("Abilities").gameObject;

        _heliceraAbility = _playerAbility.GetComponent<TwoMeleeAttack>();
        _handDamageAbility = _playerAbility.GetComponent<OneMeleeAttack>();

        _heliceraAbility.SecondAbilityEvent += damage => ApplyPoison(_heliceraAbility);
        _handDamageAbility.FirstAbilityEvent += damage => ApplyPoison(_handDamageAbility);
    }

    private void Update()
    {
        if (_toggleTalent.isOn)
        {
            if (!_poisonIcon.activeSelf)
            {
                _poisonIcon.SetActive(true);
            }

            AbilityCheck();

            if(_timer <= 0f && _textCooldown.gameObject.activeSelf)
            {
                _textCooldown.gameObject.SetActive(false);
            }
        }
    }

    private void AbilityCheck()
    {
        if(_handDamageAbility != null && _handDamageAbility.IsActiveAbility)
        {
            UpdatePoison(4);
        }

        if(_heliceraAbility != null && _heliceraAbility.IsActiveAbility)
        {
            UpdatePoison(7);
        }
    }

    private void UpdatePoison(int poisonInterval)
    {
        _timer += Time.deltaTime;

        if ((int)_timer >= 0)
        {
            _textCooldown.gameObject.SetActive(true);
            _textCooldown.text = (poisonInterval - (int)_timer).ToString();
        }

        if (_timer >= poisonInterval)
        {
            _timer = 0f;

            // Увеличивает poison на 1, но не более 2
            Poison = Mathf.Min(Poison + 1, 2);
            _textPoison.text = Poison.ToString();
        }
    }

     public void ApplyPoison(AbilityBase ability)
    {
        if (Poison > 0)
        {
            if (Random.value <= _poisonChance)
            {
                _newPrefab = Instantiate(_poisonDebuff);
                _newPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(2);
                _newPrefab.transform.SetParent(ability.TargetParent.transform);
                _newPrefab.GetComponent<Poison>().Player = _player;

                if(PsionicaValue > 0)
                {
                    _newPrefab.GetComponent<Poison>().PsionicaValue = PsionicaValue;
                }

                _newPrefab.GetComponent<Poison>().AddPoison(2f);
                _newPrefab = null;

                Poison -= 1;
                _textPoison.text = Poison.ToString();
            }
        }
    }

    private void OnDisable()
    {
        _heliceraAbility.SecondAbilityEvent -= damage => ApplyPoison(_heliceraAbility);
        _handDamageAbility.FirstAbilityEvent -= damage => ApplyPoison(_handDamageAbility);
    }
}
