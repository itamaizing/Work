using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Debug
{
    public class DebugDamageModuleUI : DebugUnitModuleUI<DebugDamageModule>
    {
        [SerializeField] private TMP_Dropdown _damageTypeDropdown;
        [SerializeField] private TMP_Dropdown _schoolDropdown;
        [SerializeField] private TMP_Dropdown _abilityFormDropdown;
        [SerializeField] private TMP_Dropdown _attackTypeFormDropdown;
        [SerializeField] private TMP_Dropdown _skillTypeFormDropdown;
        [SerializeField] private TMP_InputField _valueInputField;
        [SerializeField] private TMP_InputField _DamageKeyInputField;
        [SerializeField] private Toggle _fullyAbsorbedToggle;

        private void Start()
        {
            _damageTypeDropdown.FillWithEnum<DamageType>().onValueChanged.AddListener((index) => Module.DamageType = (DamageType)index);
            _schoolDropdown.FillWithEnum<Schools>().onValueChanged.AddListener((index) => Module.School = (Schools)index);
            _abilityFormDropdown.FillWithEnum<AbilityForm>().onValueChanged.AddListener((index) => Module.Form = (AbilityForm)index);
            _attackTypeFormDropdown.FillWithEnum<AttackRangeType>().onValueChanged.AddListener((index) => Module.PhysicAttackType = (AttackRangeType)index);
            _skillTypeFormDropdown.FillWithEnum<SkillType>().onValueChanged.AddListener((index) => Module.SkillType = (SkillType)index);

            _valueInputField.onEndEdit.AddListener((value) => Module.Value = float.Parse(value));
            _DamageKeyInputField.onEndEdit.AddListener((value) => Module.DamageKey = value);
            _fullyAbsorbedToggle.onValueChanged.AddListener((value) => Module.FullyAbsorbed = value);
        }
    }
}
