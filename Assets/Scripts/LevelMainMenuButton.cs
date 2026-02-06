using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LevelMainMenuButton : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(AddLevel);
    }

    private void AddLevel()
    {
        if (User.Instance == null)
        {
            Debug.LogWarning("User.Instance is null");
            return;
        }

        Debug.Log("Add Level");
        LevelCharacterManager.Instance.AddExperience(LevelCharacterManager.Instance.GetExperienceForNextLevel());
    }
}