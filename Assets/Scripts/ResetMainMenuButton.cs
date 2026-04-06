using UnityEngine;
using UnityEngine.UI;

public class ResetMainMenuButton : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(ResetAllUserData);
    }

    private void ResetAllUserData()
    {
        ResetBottleData();
        ResetLevelData();
        PlayerPrefs.Save();
    }

    private void ResetBottleData()
    {
        BottleUserManager.Instance?.ResetBottleData();
    }

    private void ResetLevelData()
    {
        var levelManager = LevelCharacterManager.Instance;

        levelManager.ResetAllLevelData();
        levelManager.DisplayCurrentHeroLevelInfo();
    }
}