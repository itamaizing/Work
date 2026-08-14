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
            CmdApplyStateToTarget(target.gameObject, States.EarthsHealth, _buffDuration, Schools.Earth, _owner.gameObject, nameof(EarthElementalHealthAura),0);
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

        private const float HealthMaxPercent = 0.10f;
        private const float HealthRegenPercent = 0.002f;
        
        private readonly AttributeModifier _maxHealthModifier =
            new AttributeModifier(HealthMaxPercent, ModifierType.Percent);
        
        private readonly AttributeModifier _healthRegenModifier =
            new AttributeModifier(HealthRegenPercent, ModifierType.Flat);

        public override States State => States.EarthsHealth;
        public override StateType Type => StateType.Magic;
        public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
        public override List<StatusEffect> Effects => _effects;

        public override void EnterState(CharacterState characterState, float durationToExit, float damageToExit,
            Character personWhoMadeBuff, string skillName)
        {
            this.characterState = characterState;

            _maxHealthModifier.Source = this;
            _healthRegenModifier.Source = this;

            ApplyBuffs();
        }

        private void ApplyBuffs()
        {
            if (characterState == null || characterState.Character == null) return;

            if (health != null)
            {
                health.AddModifier(ResourceAttributeName.MaxValue, _maxHealthModifier);
                
                _healthRegenModifier.Value = HealthRegenPercent * health.MaxValue;
                
                health.AddModifier(ResourceAttributeName.Regen, _healthRegenModifier);
            }
        }

        private void RemoveBuffs()
        {
            if (characterState == null || characterState.Character == null) return;

            if (health != null)
            {
                health.RemoveModifierBySource(ResourceAttributeName.MaxValue,this);
                health.RemoveModifierBySource(ResourceAttributeName.Regen,this);
            }
        }

        public override void ExitState()
        {
            currentStacksCount = 0;
            RemoveBuffs();
            base.ExitState();
        }

        public override bool Stack(float time) => false;

        public override void UpdateState()
        {
        }
    }
}
