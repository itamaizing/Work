using TMPro;
using UnityEngine;
using Extensions;

namespace Game.Debug
{
    public class DebugStateModuleUI : DebugUnitModuleUI<DebugStateModule>
    {
        [SerializeField] private TMP_Dropdown _stateDropdown;
        [SerializeField] private TMP_Dropdown _schoolDropdown;
        [SerializeField] private TMP_InputField _durationInputField;
        [SerializeField] private TMP_InputField _damageToExitInputField;

        private void Start()
        {
            _stateDropdown.FillWithEnum<States>().onValueChanged.AddListener( (index) => Module.State = (States)index);
            _schoolDropdown.FillWithEnum<Schools>().onValueChanged.AddListener( (index) => Module.School = (Schools)index);

            _durationInputField.onEndEdit.AddListener( (value) => Module.Duration = float.Parse(value));
            _damageToExitInputField.onEndEdit.AddListener( (value) => Module.DamageForExit = float.Parse(value));
        }
    }
}
