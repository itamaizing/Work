using System;
using UnityEngine;

public class MarkOfPredator : MonoBehaviour
{
    [SerializeField] private GameObject MarkEffect;

    public event Action<MarkOfPredator> DestroyedMark;

    public bool _isClaw;
    public bool _isChelicerae;
    public GameObject Player;
    private GameObject _playerAbility;

    void Start()
    {
        _isChelicerae = false;
        _isClaw = false;

        _playerAbility = Player.transform.Find("Abilities").gameObject;

        _playerAbility.GetComponent<OneMeleeAttack>().FirstAbilityEvent += DamageFromClaw;
        _playerAbility.GetComponent<TwoMeleeAttack>().SecondAbilityEvent += DamageFromChelicerae;
    }

    private void DamageFromClaw(float damage)
    {
        if (_playerAbility.GetComponent<OneMeleeAttack>().TargetParent == transform.parent.gameObject)
        {
            _isClaw = true;

            if (UnityEngine.Random.value < 0.3f)
            {
                _playerAbility.GetComponent<Bleeding>().StartBleed(gameObject, _playerAbility.GetComponent<OneMeleeAttack>());
            }

            CheckDamage();
        }
    }

    private void DamageFromChelicerae(float damage)
    {
        if (_playerAbility.GetComponent<TwoMeleeAttack>().Target == transform.parent.gameObject)
        {
            _isChelicerae = true;

            if (UnityEngine.Random.value < 0.3f)
            {
                _playerAbility.GetComponent<Bleeding>().StartBleed(gameObject, _playerAbility.GetComponent<TwoMeleeAttack>());
            }

            CheckDamage();
        }
    }

    private void CheckDamage()
    {
        if (_isClaw && _isChelicerae)
        {
            Destroy(MarkEffect);
            Destroy(gameObject);
        }
    }
    private void OnDestroy()
    {
        _playerAbility.GetComponent<OneMeleeAttack>().FirstAbilityEvent -= DamageFromClaw;
        _playerAbility.GetComponent<TwoMeleeAttack>().SecondAbilityEvent -= DamageFromChelicerae;
        DestroyedMark?.Invoke(this);
    }
}
