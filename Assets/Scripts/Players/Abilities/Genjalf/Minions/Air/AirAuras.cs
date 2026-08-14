using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gangdollarff.AirElemental
{
    public class AirAuras : MonoBehaviour
    {

    }

    public class Discharge : RefreshingState
    {
        private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Poison };
        private float _timeAfterLastEffect = 0;
        private float _effectRate = 1;

        private const float SlowPercent = -0.50f;
        
        private readonly AttributeModifier _moveSpeedModifier = new AttributeModifier(SlowPercent, ModifierType.Percent);

        
        private int _chance = 50;

        public override States State => States.Discharge;

        public override StateType Type => StateType.Magic;

        public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

        public override List<StatusEffect> Effects => _effects;

        public override Schools Schools => Schools.Air;

        public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
        {
            MaxStacksCount = 1;
            _moveSpeedModifier.Source = this;

            ApplySlow();
            
            //Пока не буду удалять, может пригодится
            //DischargeTick();
        }

        public override void ExitState()
        {
            currentStacksCount = 0;
            RemoveSlow();
            characterState.RemoveState(this);
        }

        public override void UpdateState()
        {
            _timeAfterLastEffect += Time.deltaTime;

            //DischargeTick();
            
            _timeAfterLastEffect = 0;
        }
        
        private void ApplySlow()
        {
            if (characterState == null || characterState.Character == null) return;
            
            var moveSpeedAttribute = characterState.Character.AttributeSystem[CharacterAttributeName.MoveSpeed];

            if (moveSpeedAttribute != null)
            {
                if (!moveSpeedAttribute.Modifiers.Contains(_moveSpeedModifier))
                {
                    moveSpeedAttribute.AddModifier(_moveSpeedModifier);
                }
            }
        }

        private void RemoveSlow()
        {
            if (characterState == null || characterState.Character == null) return;

            var moveSpeedAttribute = characterState.Character.AttributeSystem[CharacterAttributeName.MoveSpeed];

            if (moveSpeedAttribute != null)
            {
                moveSpeedAttribute.RemoveModifier(_moveSpeedModifier);
            }
        }

        private void DischargeTick()
        {
            if (_effectRate > _timeAfterLastEffect && Random.Range(1, 100) >= _chance)
                return;
            
            characterState.RemoveState(characterState.CurrentStates.FirstOrDefault(item => item.BaffDebaff == BaffDebaff.Baff));
        }
    }

    public class RisingWind : AuraState
    {
        private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Strengthening };
        private float _procent = 1.10f;
        private AttributeModifier _modif;

        public override float Distance => 6;
        public override float EffectRate => 0.2f;
        public override LayerMask LayerMask => LayerMask.GetMask("Allies");
        public override States State => States.EarthsHealth;
        public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
        public override List<StatusEffect> Effects => _effects;

        public override void EffectOnEnter(Character character)
        {
            _modif.Value = _procent;
            _modif.Type = ModifierType.Percent;
            //character.Move.SetMoveSpeed(character.Move.CurrentSpeed * _procent);
            character.Move.AddModifier(_modif);
        }

        public override void EffectOnExit(Character character)
        {
            //character.Move.SetMoveSpeed(character.Move.CurrentSpeed / _procent);
            character.Move.RemoveModifier(_modif);
        }

        public override void EffectOnStay(List<Character> characters)
        {

        }
    }
}

