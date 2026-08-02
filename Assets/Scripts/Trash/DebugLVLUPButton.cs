using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DebugLVLUPButton : MonoBehaviour
{
    [SerializeField] private SelectManager _selectManager;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(AddLevel);
    }

    private void AddLevel()
    {
        //if (User.Instance == null) return;
        if (_selectManager.SelectedControllableUnits.Count == 0) return;
        var selectedHero = _selectManager.SelectedControllableUnits[0];

        if (selectedHero == null || selectedHero.LVL == null) return;

        LevelCharacterManager.Instance.AddExperience(LevelCharacterManager.Instance.GetExperienceForNextLevel());
        selectedHero.SelectComponent.ForceSelect();
    }
}
