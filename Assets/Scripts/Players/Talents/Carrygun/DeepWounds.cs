using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeepWounds : MonoBehaviour
{
    [SerializeField] private Toggle _toggleTalent;
    public int Darts;

    private float _timer = 0f;
    private GameObject _player;
    private GameObject _playerAbility;
    private GameObject _lastBleedingTarget;

    void Start()
    {
        _player = transform.parent.gameObject;
        _playerAbility = _player.transform.Find("Abilities").gameObject;

        _playerAbility.GetComponent<TwoMeleeAttack>().SecondAbilityEvent += damage => UseDarts();

        _playerAbility.GetComponent<Bleeding>().BleedingEvent += CheckRepeatedBleeding;

    }

    void Update()
    {
        if (_toggleTalent.isOn)
        {
            UpdateDarts(5f);
        }
    }

    public void UpdateDarts(float dartsInterval)
    {
        _timer += Time.deltaTime;

        if (_timer >= dartsInterval)
        {
            _timer = 0f;

            // Увеличивает poison на 1, но не более 2
            Darts = Mathf.Min(Darts + 1, 2);
        }
    }

    public void UseDarts()
    {
        Darts -= 1;
    }

    private void CheckRepeatedBleeding(GameObject target, AbilityBase ability)
    {
        if (_toggleTalent.isOn)
        {
            float bleedingChance;

            if (ability == _playerAbility.GetComponent<OneMeleeAttack>())
            {
                bleedingChance = 0.6f;
            }
            else if (ability == _playerAbility.GetComponent<TwoMeleeAttack>())
            {
                bleedingChance = 0.3f;
            }
            else
            {
                bleedingChance = 0;
            }

            if (bleedingChance != 0 && _lastBleedingTarget != null && _lastBleedingTarget == target)
            {
                // Метод вызван повторно для той же самой цели
                if (Random.value < bleedingChance)
                {
                    target.GetComponentInChildren<BleedingDebaff>().BleedingDuration += 3f;
                    target.GetComponentInChildren<BaffDebaffEffectPrefab>().Timer += 3f;
                }
            }

            _lastBleedingTarget = target;
        }
    }
}
