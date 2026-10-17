using TMPro;
using UnityEngine;
using Extensions;

namespace Game.Debug
{
    public class DebugSkillModuleUI : DebugUnitModuleUI<DebugSkillModule>
    {
        [SerializeField] private TMP_Dropdown _comandDropdown;

        private void Start()
        {
            _comandDropdown.FillWithEnum<DebugSkillCommand>().onValueChanged.AddListener((index) => Module.Command = (DebugSkillCommand)index);
        }
    }
}
