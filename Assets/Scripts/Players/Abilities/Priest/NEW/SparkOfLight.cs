using System.Collections.Generic;
using UnityEngine;

public class SparkOfLight : AutoAttackSkill
{
    [Header("Spark Of Light Settings")] 
    [SerializeField] private float _buffDuration = 9f;
    [SerializeField] private float _healAmount = 2f;
    [SerializeField] private float _damageAmount = 20f;
    [SerializeField] private float _castTime = 0.8f;
    [SerializeField] private float _range = 4f;
    [SerializeField] private List<SkillEnergyCost> _manaCostHeal;
    [SerializeField] private List<SkillEnergyCost> _manaCostDamage;

    protected override void CastAction()
    {
        Debug.Log("CastAction вызван. Проверяем цель...");

        if (_target == null)
        {
            Debug.LogError("Цель отсутствует! CastAction отменён.");
            return;
        }

        bool isAlly = _target.gameObject.layer == LayerMask.NameToLayer("Allies");
        bool isEnemy = _target.gameObject.layer == LayerMask.NameToLayer("Enemy");

        Debug.Log($"Цель: {_target.name}, Союзник: {isAlly}, Враг: {isEnemy}");

        if (isAlly && TryPayCost(_manaCostHeal))
        {
            Debug.Log("Начинаем лечение союзника...");
            Heal(_target);
            ApplySpiritEnergyBuff(_target);
        }
        else if (isEnemy && TryPayCost(_manaCostDamage))
        {
            Debug.Log("Начинаем атаку на врага...");
            Damage(_target);
        }
        else
        {
            Debug.LogWarning("Цель не подходит для лечения или атаки, либо недостаточно маны.");
        }
    }

    private void Heal(Character target)
    {
        Debug.Log($"Попытка исцелить цель: {target.name}");

        var healthComponent = target.GetComponent<Health>();
        if (healthComponent != null)
        {
            healthComponent.Heal(_healAmount);
            Debug.Log($"Союзник {target.name} вылечен на {_healAmount} ед. здоровья.");
        }
        else
        {
            Debug.LogError($"Компонент Health не найден на объекте {target.name}. Лечение невозможно.");
        }
    }

    private void Damage(Character target)
    {
        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damageAmount),
            Type = DamageType.Magical,
            Range = AttackRangeType.RangeAttack
        };

        CmdApplyDamage(damage, target.gameObject);
    }

    private void ApplySpiritEnergyBuff(Character target)
    {
        Debug.Log($"Попытка наложить бафф 'Spirit Energy' на цель: {target.name}");

        if (target.TryGetComponent<CharacterState>(out var characterState))
        {
            characterState.CmdAddState(States.SpiritEnergy, _buffDuration, 0, target.gameObject, "SparkOfLight");
            Debug.Log($"Бафф 'Spirit Energy' успешно наложен на {target.name} на {_buffDuration} секунд.");
        }
        else
        {
            Debug.LogError($"Компонент CharacterState не найден на объекте {target.name}. Наложение баффа невозможно.");
        }
    }

    protected override void ClearData()
    {
        Debug.Log("Очистка данных...");
        base.ClearData();
        Debug.Log("Данные успешно очищены.");
    }
}