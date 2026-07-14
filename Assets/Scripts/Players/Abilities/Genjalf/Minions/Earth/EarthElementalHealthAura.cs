using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Gangdollarff.EarthElemental
{
    public class EarthElementalHealthAura : AuraStateHandler
    {
        [SerializeField] private float _buffDuration = -1f;

        protected override void OnTargetEnter(Character target)
        {
            CmdApplyStateToTarget(target.gameObject, States.EarthsHealth, _buffDuration, Schools.Earth, _owner.gameObject, nameof(EarthElementalHealthAura));
        }

        protected override void OnTargetExit(Character target)
        {
            CmdRemoveStateFromTarget(target.gameObject, States.EarthsHealth);
        }

        protected override void OnAuraDisabled()
        {
            RemoveEffectsFromAllTargets();
        }
    }

    public class EarthsHealthBuff : AbstractCharacterState
    {
        private List<StatusEffect> _effects = new();

        private readonly Dictionary<Character, float> _charactersMaxHealth = new();
        
        private float _healthRegenProcent = 0.002f;
        private float _healthMaxProcent = 0.1f;
        private float _originalRegenValue = 0;
        private float _currentDelta = 0;

        private Character _character;
        private Resource _health;

        public override States State => States.EarthsHealth;
        public override StateType Type => StateType.Magic;
        public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
        public override List<StatusEffect> Effects => _effects;

        protected override void OnEnterState(CharacterState characterState, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
        {
            _character = characterState.Character;

            float initialMaxHealth = _character.Health.MaxValue;
            _charactersMaxHealth[_character] = initialMaxHealth;

            if (_character.Resources.Count > 0)
            {
                _character.Resources.TryGetValue(ResourceType.Health, out _health);
                if (_health != null)
                {
                    _originalRegenValue = _health.RegenerationValue;
                    _health.RegenerationValue += _health.MaxValue * _healthRegenProcent;
                    _currentDelta = _health.MaxValue * _healthMaxProcent;
                    _health.AddMax(_currentDelta,true);
                }
            }
        }
        
        private void RestoreHealth()
        {
            if (_health != null)
            {
                _health.RegenerationValue = _originalRegenValue;
                _health.AddMax(-_currentDelta,true);
            }
        }

        protected override void OnExitState()
        {           
            RestoreHealth();

            _health = null;
            _character = null;
        }

       /* public override bool Stack(float time)
        {
            return false;
        }*/

        public override void OnUpdateState()
        {
        }
    }
}
