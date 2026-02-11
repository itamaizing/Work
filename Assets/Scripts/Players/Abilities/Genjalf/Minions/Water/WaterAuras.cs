using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;


namespace Gangdollarff.WaterElemental
{
    public class WaterAuras : MonoBehaviour
    {
        private void Start()
        {
            //var chatacter = GetComponent<Character>();
            //chatacter.CharacterState.CmdAddState(States.MagicWater, 0, 0, chatacter.gameObject, name);
            //chatacter.CharacterState.CmdAddState(States.CoolingAura, 0, 0, chatacter.gameObject, name);
        }
    }

    public class MagicWater : AuraState
    {
        private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Strengthening };
        private float _procent = 0.03f;

        public override float Distance => 8;
        public override float EffectRate => 0.2f;
        public override LayerMask LayerMask => LayerMask.GetMask("Allies");
        public override States State => States.EarthsHealth;
        public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
        public override List<StatusEffect> Effects => _effects;

        public override void EffectOnEnter(Character character)
        {
            if (character.Resources.Count > 0)
                character.Health.IncreaseRegen(character.Resources[0].MaxValue * _procent);
        }

        public override void EffectOnExit(Character character)
        {
            if (character.Resources.Count > 0)
                character.Health.DecreaseRegen(character.Resources[0].MaxValue * _procent);
        }

        public override void EffectOnStay(List<Character> characters)
        {

        }
    }

    public class CoolingAura : AuraState
    {
        private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Strengthening };
        
        private float _procent = 0.03f;
        private float _currentDistance = 1f;
        private float _elapsedTime = 0f;
        private const float MIN_DISTANCE = 1f;
        private const float MAX_DISTANCE = 5f;

        public override float Distance => _currentDistance;
        public override float EffectRate => 1f;
        public override LayerMask LayerMask => LayerMask.GetMask("Enemy");
        public override States State => States.Cooling;
        public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
        public override List<StatusEffect> Effects => _effects;
        
        public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
        {
            base.EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
            _currentDistance = MIN_DISTANCE;
            _elapsedTime = 0f;
        }

        public override void UpdateState()
        {
            _elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsedTime / duration);
            _currentDistance = Mathf.Lerp(MIN_DISTANCE, MAX_DISTANCE, progress);

            base.UpdateState();

            if (_elapsedTime >= duration)
            {
                ExitState();
            }
        }

        public override void EffectOnEnter(Character character)
        {
        }

        public override void EffectOnExit(Character character)
        {
            
        }

        public override void EffectOnStay(List<Character> characters)
        {
            foreach (Character character in characters)
            {
                CmdAddState(character.gameObject);
            }
        }
        
        [Command]
        private void CmdAddState(GameObject target)
        {
            if(target != null)
                target.GetComponent<Character>().CharacterState.AddState(States.Cooling, 8, 0, target.gameObject, nameof(Cooling));
        }
    }
}
