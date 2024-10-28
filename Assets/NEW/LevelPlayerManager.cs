using UnityEngine;

public class LevelPlayerManager : MonoBehaviour
{
    private static LevelPlayerManager _instance;
    public static LevelPlayerManager Instance => _instance;

    private HeroComponent _character;
    private int _currentSaveGroup = 0;

    private int currentLevel = 1;
    private int currentExperience = 0;
    private int experienceForNextLevel = 10;

    private int additionalExperienceForNextLevel = 10;
    private float multiplierToExperienceForNextLevel = 1.2f;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void SetHero(HeroComponent hero)
    {
        _character = hero;
        LoadLevelData();
        DisplayCurrentHeroLevelInfo();
    }

    public void SetSaveIndex(int index)
    {
        _currentSaveGroup = index;
        LoadLevelData();
    }

    public void AddExperience(int experience)
    {
        if (_character == null) return;

        currentExperience += experience;
        CheckForLevelUp();
        SaveLevelData();
    }

    private void CheckForLevelUp()
    {
        while (currentExperience >= experienceForNextLevel)
        {
            currentExperience -= experienceForNextLevel;
            currentLevel++;
            experienceForNextLevel = CalculateExperienceForNextLevel();
        }
    }

    private int CalculateExperienceForNextLevel()
    {
        return Mathf.CeilToInt(experienceForNextLevel * multiplierToExperienceForNextLevel) + additionalExperienceForNextLevel;
    }

    public int GetCurrentLevel() => currentLevel;
    public int GetCurrentExperience() => currentExperience;
    public int GetExperienceForNextLevel() => experienceForNextLevel;

    private void SaveLevelData()
    {
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_Level", currentLevel);
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_Experience", currentExperience);
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_ExperienceForNextLevel", experienceForNextLevel);
        PlayerPrefs.Save();
    }

    private void LoadLevelData()
    {
        currentLevel = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_Level", 1);
        currentExperience = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_Experience", 0);
        experienceForNextLevel = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_ExperienceForNextLevel", 10);
    }

    public void DisplayCurrentHeroLevelInfo()
    {
        if (_character == null) return;
        Debug.Log($"Character: {_character.Data.Name} | Level: {currentLevel} | Experience: {currentExperience}/{experienceForNextLevel}");
    }
}
