using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;


namespace Gangdollarff.WaterElemental
{
    public class WaterAuras : AuraStateHandler
    {
        [SerializeField] private float _buffDuration = -1f;

        protected override void OnTargetEnter(Character target)
        {
            CmdApplyStateToTarget(target.gameObject,States.MagicWater, _buffDuration, Schools.Water, _owner.gameObject, nameof(MagicWater));
        }

        protected override void OnTargetExit(Character target)
        {
            CmdRemoveStateFromTarget(target.gameObject, States.MagicWater);
        }

        protected override void OnAuraDisabled()
        {
            RemoveEffectsFromAllTargets();
        }
    }

    public class MagicWater : AbstractCharacterState
    {
        private Character _character;
        private Resource _mana;
        private List<StatusEffect> _effects = new List<StatusEffect>();
        private float _manaRegenProcent = 0.003f;
        private float _manaMaxProcent = 0.1f;
        
        private float _originalRegenValue = 0;
        private float _originalMaxValue = 0;
        private float _currentDelta = 0;

        public override States State => States.MagicWater;
        public override StateType Type { get; }
        public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
        public override List<StatusEffect> Effects => _effects;
        
        public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
        {
            duration = durationToExit;
            _character = character.Character;
            if (_character.Resources.Count > 0)
            {
                _character.Resources.TryGetValue(ResourceType.Mana, out _mana);
                if (_mana != null)
                {
                    _originalRegenValue = _mana.RegenerationValue;
                    _originalMaxValue = _mana.MaxValue;
                    _mana.RegenerationValue += _mana.MaxValue * _manaRegenProcent;
                    _currentDelta = _mana.MaxValue * _manaMaxProcent;
                    _mana.AddMax(_currentDelta);
                }
            }
        }
        
        private void RestoreMana()
        {
            if (_mana != null)
            {
                _mana.RegenerationValue = _originalRegenValue;
                _mana.AddMax(-_currentDelta);
            }
        }

        public override void UpdateState() { }

        public override void ExitState()
        {
            base.ExitState();
            RestoreMana();

            _mana = null;
            _character = null;
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

    public class CoolingDamaged : AbstractCharacterState
    {
        private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Others };

        private float _physResistPercent = 0.1f;
        private float _savedPhysResist;

        public override States State => States.CoolingDamaged;
        public override StateType Type => StateType.Magic;
        public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
        public override List<StatusEffect> Effects => _effects;

        public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
            Character personWhoMadeBuff, string skillName)
        {
            _savedPhysResist = character.Character.Health.DefPhysDamage;
            character.Character.Health.SetPhysicDef(
                _savedPhysResist + _savedPhysResist * _physResistPercent);

            character.Character.Health.DamageTaken += OnDamageTaken;
        }

        private void OnDamageTaken(Damage damage, Skill skill)
        {
            if (skill == null) return;
            if (damage.Type != DamageType.Physical) return;
            if (damage.PhysicAttackType != AttackRangeType.MeleeAttack) return;

            skill.Hero.CharacterState.AddState(States.Cooling, 6f, 0,
                characterState.Character.gameObject, nameof(Cooling));
        }

        public override void UpdateState() { }

        public override void ExitState()
        {
            if (characterState?.Character != null)
            {
                characterState.Character.Health.SetPhysicDef(_savedPhysResist);
                characterState.Character.Health.DamageTaken -= OnDamageTaken;
            }
            
            _savedPhysResist = 0f;
            characterState?.RemoveState(this);
        }
    }
}
