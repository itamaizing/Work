using Game.Debug;
using TMPro;
using UnityEngine;

public class DebugSkillModuleUI : DebugUnitModuleUI<DebugSkillModule>
{
    [SerializeField] private TMP_Dropdown _comandDropdown;

    private void Start()
    {
        FillDropdownWithEnum<DebugSkillCommand>(_comandDropdown);
        _comandDropdown.onValueChanged.AddListener((index) => Module.Command = (DebugSkillCommand)index);
    }

    protected override DebugSkillModule CreateModule()
    {
        return new DebugSkillModule();
    }
}
