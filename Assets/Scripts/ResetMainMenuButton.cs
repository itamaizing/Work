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
        if (User.Instance == null) return;

        ResetBottleData();
        ResetLevelData();
        PlayerPrefs.Save();
    }

    private void ResetBottleData()
    {
        string userKey = BottleUserManager.Instance?.GetType()
            .GetField("_currentUser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(BottleUserManager.Instance) as string;

        if (string.IsNullOrEmpty(userKey)) return;

        PlayerPrefs.DeleteKey(userKey + "_Bottles");
        PlayerPrefs.DeleteKey(userKey + "_BottleVolume");

        var fieldBottles = typeof(BottleUserManager).GetField("_currentBottles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fieldVolume = typeof(BottleUserManager).GetField("_currentBottleVolume", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        fieldBottles?.SetValue(BottleUserManager.Instance, 0);
        fieldVolume?.SetValue(BottleUserManager.Instance, 0f);

        BottleUserManager.Instance.BottlesChanged(0);
    }


    private void ResetLevelData()
    {
        var levelManager = LevelCharacterManager.Instance;

        levelManager.ResetLevelData();
        levelManager.LevelChanged();

        levelManager.DisplayCurrentHeroLevelInfo();
    }
}