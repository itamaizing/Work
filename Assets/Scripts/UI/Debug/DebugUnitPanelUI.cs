using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Debug
{
    public class DebugUnitPanelUI : MonoBehaviour
    {
        [SerializeField] private SelectManager _selectManager;
        [SerializeField] private DebugUnitModuleUI[] _windows;

        private void OnEnable()
        {
            _selectManager.CharacterSelected += OnCharacterSelected;
        }

        private void OnDisable()
        {
            _selectManager.CharacterSelected -= OnCharacterSelected;
        }

        [ContextMenu(nameof(UpdateWindowsList))]
        public void UpdateWindowsList()
        {
            var list = GetComponentsInChildren<DebugUnitModuleUI>();
            _windows = list.ToArray();
        }

        private void OnCharacterSelected(Character character)
        {
            foreach (var ui in _windows)
                ui.SetUnit(character);
        }
    }

    public abstract class DebugUnitModuleUI : MonoBehaviour
    {
        public abstract void SetUnit(Character character);
    }

    public abstract class DebugUnitModuleUI<TModule> : DebugUnitModuleUI where TModule : DebugUnitModule
    {
        [SerializeField]
        private Button _executeButton;
        private Character _unit;

        protected TModule Module { get; private set; }

        protected abstract TModule CreateModule();

        private void Awake() => Module = CreateModule();
        private void OnEnable() => _executeButton.onClick.AddListener(OnClick);
        private void OnDisable() => _executeButton.onClick.RemoveListener(OnClick);

        public override void SetUnit(Character character) => _unit = character;

        protected void FillDropdownWithEnum<TEnum>(TMP_Dropdown dropdown) where TEnum : Enum
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(Enum.GetNames(typeof(TEnum)).ToList());
            dropdown.value = 0;
        }

        private void OnClick() => Module.Execute(_unit);
    }

    public abstract class DebugUnitModule
    {
        public void Execute(Character unit)
        {
            if (unit == null)
            {
                UnityEngine.Debug.LogWarning($"unit is null");
                return;
            }
            ExecuteInternal(unit);
        }

        protected abstract void ExecuteInternal(Character unit);
    }

    public class DebugStateModule : DebugUnitModule
    {
        private const string SKILL_NAME = "None(debug)";

        public States State { get; set; }
        public float Duration { get; set; }
        public float DamageForExit { get; set; }
        public Schools School { get; set; }

        protected override void ExecuteInternal(Character unit)
        {
            unit.CharacterState.CmdAddState(State, Duration, DamageForExit, School, unit.gameObject, SKILL_NAME);
        }
    }

    public enum DebugSkillCommand
    {
        None,
        CooldownReset,
        EnableTargetingAll
    }

    public class DebugSkillModule : DebugUnitModule
    {
        public DebugSkillCommand Command { get; set; }

        protected override void ExecuteInternal(Character unit)
        {
            switch (Command)
            {
                case DebugSkillCommand.None:
                    break;

                case DebugSkillCommand.CooldownReset:
                    OnCooldownReset(unit);
                    break;

                case DebugSkillCommand.EnableTargetingAll:
                    OnEnableTargetingAll(unit);
                    break;
            }
        }

        private void OnCooldownReset(Character unit)
        {
            foreach (var skill in unit.Abilities.Abilities)
                skill.CooldownReset();
        }

        private void OnEnableTargetingAll(Character unit)
        {
            foreach (var skill in unit.Abilities.Abilities)
            {
                skill.CmdSetTargetingAll();
                skill.Targeting.Faction = TargetFaction.All;
            }     
        }
    }

    public enum DebugAttributesCommand
    {
        None,
        Health,
        HealthRegen,
        Mana,
        ManaRegen,
        Team,
        TalentPoints
    }

    public class DebugAttributesModule : DebugUnitModule
    {
        public float Value { get; set; }
        public DebugAttributesCommand Command { get; set; }

        protected override void ExecuteInternal(Character unit)
        {
            switch (Command)
            {
                case DebugAttributesCommand.None:
                    break;

                case DebugAttributesCommand.Health:
                    OnSetHealth(unit);
                    break;

                case DebugAttributesCommand.HealthRegen:
                    OnSetHealthRegen(unit);
                    break;

                case DebugAttributesCommand.Mana:
                    OnSetMana(unit);
                    break;

                case DebugAttributesCommand.ManaRegen:
                    OnSetManaRegen(unit);
                    break;

                case DebugAttributesCommand.Team:
                    OnSwitchTeam(unit);
                    break;

                case DebugAttributesCommand.TalentPoints:
                    OnSetTalantPoints(unit);
                    break;

                default:
                    break;
            }
        }

        private void OnSetHealth(Character unit)
        {
            unit.Health.CmdSetValue(Value);
        }

        private void OnSetHealthRegen(Character unit)
        {
            unit.Health.CmdSetRegenValue(Value);
        }

        private void OnSetMana(Character unit)
        {
            unit.Resource.CmdSetValue(Value); ;
        }

        private void OnSetManaRegen(Character unit)
        {
            unit.Resource.CmdSetRegenValue(Value);
        }

        private void OnSwitchTeam(Character unit)
        {
            unit.NetworkSettings.CmdSetTeam((byte)Value);
        }

        private void OnSetTalantPoints(Character unit)
        {
            unit.Abilities.TalesntSystem.SetPoints((int)Value);
        }
    }

    public class DebugDamageModule : DebugUnitModule
    {
        public float Value { get; set; }
        public DamageType DamageType { get; set; }
        public Schools School { get; set; }
        public AbilityForm Form { get; set; }
        public AttackRangeType PhysicAttackType { get; set; }
        public SkillType SkillType { get; set; }
        public string DamageKey { get; set; }
        public bool FullyAbsorbed { get; set; }

        protected override void ExecuteInternal(Character unit)
        {
            var damage = new Damage()
            {
                Value = Value,
                Type = DamageType,
                School = School,
                Form = Form,
                PhysicAttackType = PhysicAttackType,
                SkillType = SkillType,
                DamageKey = DamageKey,
                FullyAbsorbed = FullyAbsorbed
            };
            unit.CmdTryTakeDamage(damage, null);
        }
    }
}
