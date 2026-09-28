using Game.Debug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        FillDropdownWithEnum<DamageType>(_damageTypeDropdown);
        _damageTypeDropdown.onValueChanged.AddListener((index) => Module.DamageType = (DamageType)index);

        FillDropdownWithEnum<Schools>(_schoolDropdown);
        _schoolDropdown.onValueChanged.AddListener((index) => Module.School = (Schools)index);

        FillDropdownWithEnum<AbilityForm>(_abilityFormDropdown);
        _abilityFormDropdown.onValueChanged.AddListener((index) => Module.Form = (AbilityForm)index);

        FillDropdownWithEnum<AttackRangeType>(_attackTypeFormDropdown);
        _attackTypeFormDropdown.onValueChanged.AddListener((index) => Module.PhysicAttackType = (AttackRangeType)index);

        FillDropdownWithEnum<SkillType>(_skillTypeFormDropdown);
        _skillTypeFormDropdown.onValueChanged.AddListener((index) => Module.SkillType = (SkillType)index);

        _valueInputField.onEndEdit.AddListener((value) => Module.Value = float.Parse(value));
        _DamageKeyInputField.onEndEdit.AddListener((value) => Module.DamageKey = value);
        _fullyAbsorbedToggle.onValueChanged.AddListener((value) => Module.FullyAbsorbed = value);
    }

    protected override DebugDamageModule CreateModule()
    {
        return new DebugDamageModule();
    }
}
