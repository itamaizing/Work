using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Gangdollarff.EarthElemental
{
    public class EarthElementalAuras : AuraStateHandler
    {
        private void Start()
        {
            var chatacter = GetComponent<Character>();
            //chatacter.CharacterState.CmdAddState(States.PowerOfEarth, 0, 0, chatacter.gameObject, name);
            chatacter.CharacterState.CmdAddState(States.EarthsHealth, 0, 0, chatacter.gameObject, name);
        }
        
        [SerializeField] private float _buffDuration = -1f;

        protected override void OnTargetEnter(Character target)
        {
            CmdApplyStateToTarget(target.gameObject, States.EarthsHealth, _buffDuration, Schools.Earth, _owner.gameObject, nameof(EarthElementalAuras));
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

    public class PowerOfEarth : AuraState
    {
        private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Others };
        private int _stanChance = 20;
        private float _stanDuration = 0.1f;
        private float _addDamage = .5f;

        private HashSet<Character> _subscribedCharacters = new();

        public override float Distance => 1.6f;
        public override float EffectRate => 0.2f;
        public override LayerMask LayerMask => LayerMask.GetMask("Allies");
        public override States State => States.PowerOfEarth;
        public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
        public override List<StatusEffect> Effects => _effects;

        public override void EffectOnEnter(Character character)
        {
            character.DamageGeted += OnDamageGeted;
            _subscribedCharacters.Add(character);
        }

        public override void EffectOnExit(Character character)
        {
            character.DamageGeted -= OnDamageGeted;
        }

        public override void EffectOnStay(List<Character> characters)
        {
            foreach (var character in characters)
            {
                if (_subscribedCharacters.Contains(character)) continue;
                character.DamageGeted += OnDamageGeted;
                _subscribedCharacters.Add(character);
            }
        }

        public override void ExitState()
        {
            foreach (var character in _subscribedCharacters)
            {
                if (character != null)
                    character.DamageGeted -= OnDamageGeted;
            }

            _subscribedCharacters.Clear();
            base.ExitState();
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

    public class EarthsHealthBuff : AbstractCharacterState
    {
        private List<StatusEffect> _effects = new();

        private readonly Dictionary<Character, float> _charactersMaxHealth = new();

        private float _percent = 0.02f;
        private AttributeModifier _modifier = new(0.3f, ModifierType.Percent);

        private Character _character;

        public override States State => States.EarthsHealth;
        public override StateType Type => StateType.Magic;
        public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
        public override List<StatusEffect> Effects => _effects;

        public override void EnterState(CharacterState characterState, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
        {
            _character = characterState.Character;

            float initialMaxHealth = _character.Health.MaxValue;
            _charactersMaxHealth[_character] = initialMaxHealth;

            _character.Health.IncreaseRegen(initialMaxHealth * _percent);
            _character.Health.AddModifier(_modifier);
        }

        public override void ExitState()
        {
            characterState.RemoveState(this);
            
            if (_character == null) return;

            if (_charactersMaxHealth.TryGetValue(_character, out var maxHealth))
            {
                _character.Health.DecreaseRegen(maxHealth * _percent);
                _character.Health.RemoveModifier(_modifier);

                _charactersMaxHealth.Remove(_character);
            }
        }

        public override bool Stack(float time)
        {
            return false;
        }

        public override void UpdateState()
        {
        }
    }
}
