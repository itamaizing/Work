using TMPro;
using UnityEngine;
using Extensions;

namespace Game.Debug
{
    public class DebugAttributesModuleUI : DebugUnitModuleUI<DebugAttributesModule>
    {
        [SerializeField] private TMP_Dropdown _comandDropdown;
        [SerializeField] private TMP_InputField _valueDropdown;

        private void Start()
        {
            _comandDropdown.FillWithEnum<DebugAttributesCommand>().onValueChanged.AddListener((index) => Module.Command = (DebugAttributesCommand)index);
            _valueDropdown.onValueChanged.AddListener((value) => Module.Value = float.Parse(value));
        }
    }
}
