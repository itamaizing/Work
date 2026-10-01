using Game.Debug;
using TMPro;
using UnityEngine;

public class DebugAttributesModuleUI : DebugUnitModuleUI<DebugAttributesModule>
{
    [SerializeField] private TMP_Dropdown _comandDropdown;
    [SerializeField] private TMP_InputField _valueDropdown;

    private void Start()
    {
        FillDropdownWithEnum<DebugAttributesCommand>(_comandDropdown);
        _comandDropdown.onValueChanged.AddListener((index) => Module.Command = (DebugAttributesCommand)index);

        _valueDropdown.onValueChanged.AddListener((value) => Module.Value = float.Parse(value));
    }

    protected override DebugAttributesModule CreateModule()
    {
        return new DebugAttributesModule();
    }
}
