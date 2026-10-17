using System.Linq;
using UnityEngine;

namespace Game.Debug
{
    public class DebugPanelUI : MonoBehaviour
    {
        [SerializeField] private SelectManager _selectManager;
        [SerializeField] private DebugUnitModuleUI[] _unitModuleWindows;

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
            _unitModuleWindows = list.ToArray();
        }

        private void OnCharacterSelected(Character character)
        {
            UnityEngine.Debug.Log($"¬€¡–¿Õ - {character.gameObject.name}");
            foreach (var ui in _unitModuleWindows)
                ui.SetUnit(character);
        }
    }
}
