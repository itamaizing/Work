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

        private int _chance = 50;

        public override States State => States.Discharge;

        public override StateType Type => StateType.Magic;

        public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

        public override List<StatusEffect> Effects => _effects;

        public override Schools Schools => Schools.Air;

        public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
        {
        }

        public override void ExitState()
        {
            characterState.RemoveState(this);
        }

        public override bool Stack(float time)
        {
            return false;
        }

        public override void UpdateState()
        {

            _timeAfterLastEffect += Time.deltaTime;

            if (_effectRate > _timeAfterLastEffect && Random.Range(1, 100) >= _chance)
                return;

            //
            characterState.RemoveState(characterState.CurrentStates.FirstOrDefault(item => item.BaffDebaff == BaffDebaff.Baff));

            _timeAfterLastEffect = 0;
        }
        public override void ReduceStack()
        {
            ExitState();
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

