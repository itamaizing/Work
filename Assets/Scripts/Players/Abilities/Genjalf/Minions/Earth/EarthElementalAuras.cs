using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Gangdollarff.EarthElemental
{
    public class EarthElementalAuras : MonoBehaviour
    {
        private void Start()
        {
            var chatacter = GetComponent<Character>();
            //chatacter.CharacterState.CmdAddState(States.PowerOfEarth, 0, 0, chatacter.gameObject, name);
            chatacter.CharacterState.CmdAddState(States.EarthsHealth, 0, 0, chatacter.gameObject, name);
        }
    }

    public class PowerOfEarth : AuraState
    {
        private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Others };
        private int _stanChance = 20;
        private float _stanDuration = 0.1f;
        private float _addDamage = .5f;

        public override float Distance => 1.6f;
        public override float EffectRate => 0.2f;
        public override LayerMask LayerMask => LayerMask.GetMask("Allies");
        public override States State => States.PowerOfEarth;
        public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
        public override List<StatusEffect> Effects => _effects;

        public override void EffectOnEnter(Character character)
        {
            character.DamageGeted += OnDamageGeted;
        }

        public override void EffectOnExit(Character character)
        {
            character.DamageGeted -= OnDamageGeted;
        }

        public override void EffectOnStay(List<Character> characters)
        {
            
        }

        private void OnDamageGeted(Damage damage, GameObject character)
        {
            var randomInt = Random.Range(0, 100);

            if (damage.PhysicAttackType != AttackRangeType.MeleeAttack || randomInt > _stanChance)
                return;

            if (character.TryGetComponent(out Character target))
            {
                target.CharacterState.AddState(States.Stun, damage.Value * _stanDuration, 0, character, "name");

                damage.Value *= _addDamage;
                target.TryTakeDamage(ref damage, null);
            }
        }
    }

    public class EarthsHealth : AuraState
    {
        private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Strengthening };
        private Dictionary<Character, float> _charactersMaxHealth = new();
        private float _procent = 0.02f;
        private float _maxValuePercent = 0.1f;

        public override float Distance => 6;
        public override float EffectRate => 0.2f;
        public override LayerMask LayerMask => LayerMask.GetMask("Allies");
        public override States State => States.EarthsHealth;
        public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
        public override List<StatusEffect> Effects => _effects;

        public override void EffectOnEnter(Character character)
        {
            float initialMaxHealth = character.Health.MaxValue;
            _charactersMaxHealth.Add(character,initialMaxHealth);
            
            character.Health.IncreaseRegen(_charactersMaxHealth[character] * _procent);
            //character.Health.ChangedMaxValue(_charactersMaxHealth[character] * _maxValuePercent); //TODO: Переписать на атрибут
        }

        public override void EffectOnExit(Character character)
        {
            character.Health.DecreaseRegen(_charactersMaxHealth[character] * _procent);
            //character.Health.ChangedMaxValue(-(_charactersMaxHealth[character] * _maxValuePercent)); //TODO: Переписать на атрибут
            _charactersMaxHealth.Remove(character);
        }

        public override void EffectOnStay(List<Character> characters)
        {
            
        }
    }
}
