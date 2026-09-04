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
        private const float TickInterval = 1f;
        
        private readonly AttributeModifier _maxHealthModifier =
            new AttributeModifier(HealthMaxPercent, ModifierType.Percent);
        
        private Coroutine _regenCoroutine;

        public override States State => States.EarthsHealth;
        public override StateType Type => StateType.Magic;
        public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
        public override List<StatusEffect> Effects => _effects;

        public override void EnterState(CharacterState characterState, float durationToExit, float damageToExit,
            Character personWhoMadeBuff, string skillName)
        {
            this.characterState = characterState;
            health = characterState.Character?.Health;

            _maxHealthModifier.Source = this;

            ApplyBuffs();
            StartRegenRoutine();
        }

        private void ApplyBuffs()
        {
            if (characterState == null || characterState.Character == null) return;

            if (health != null)
            {
                health.AddModifier(ResourceAttributeName.MaxValue, _maxHealthModifier);
            }
        }

        private void RemoveBuffs()
        {
            if (characterState == null || characterState.Character == null) return;

            if (health != null)
            {
                health.RemoveModifierBySource(ResourceAttributeName.MaxValue, this);
            }
        }
        
        private void StartRegenRoutine()
        {
            if (characterState?.Character == null) return;
            
            if (characterState.Character.isServer || characterState.Character.isServerOnly)
            {
                _regenCoroutine = characterState.StartCoroutine(RegenRoutine());
            }
        }

        private void StopRegenRoutine()
        {
            if (_regenCoroutine != null && characterState != null)
            {
                characterState.StopCoroutine(_regenCoroutine);
                _regenCoroutine = null;
            }
        }

        private IEnumerator RegenRoutine()
        {
            var waitForInterval = new WaitForSeconds(TickInterval);

            while (true)
            {
                yield return waitForInterval;

                if (health != null)
                {
                    float regenAmount = health.MaxValue * HealthRegenPercent;
                    if (regenAmount > 0)
                    {
                        health.Add(regenAmount);
                    }
                }
            }
        }

        public override void ExitState()
        {
            currentStacksCount = 0;
            StopRegenRoutine();
            RemoveBuffs();
            base.ExitState();
        }


        public override bool Stack(float time) => false;

        public override void UpdateState()
        {
        }
    }
}
