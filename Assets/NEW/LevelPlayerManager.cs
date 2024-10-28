using UnityEngine;

public class LevelPlayerManager : MonoBehaviour
{
    private static LevelPlayerManager _instance;
    public static LevelPlayerManager Instance => _instance;

    private HeroComponent _character;
    private int _currentSaveGroup = 0;

    private int currentLevel = 1;
    private int currentExperience = 0;
    private int experienceForNextLevel = 100;

    private const int maxLevel = 9;
    private const int maxExperienceAtMaxLevel = 800;

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
        //ResetLevelData();
    }

    public void SetSaveIndex(int index)
    {
        _currentSaveGroup = index;
        LoadLevelData();
    }

    public void AddExperience(int experience)
    {
        if (_character == null || currentLevel >= maxLevel) return;

        currentExperience += experience;
        CheckForLevelUp();
        SaveLevelData();
    }

    private void CheckForLevelUp()
    {
        while (currentExperience >= experienceForNextLevel && currentLevel < maxLevel)
        {
            currentExperience -= experienceForNextLevel;
            currentLevel++;
            experienceForNextLevel = CalculateExperienceForNextLevel();

            if (currentLevel == maxLevel)
            {
                currentExperience = maxExperienceAtMaxLevel;
                experienceForNextLevel = maxExperienceAtMaxLevel;
                break;
            }
        }
    }

    private int CalculateExperienceForNextLevel()
    {
        return currentLevel * 100;
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
        experienceForNextLevel = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_ExperienceForNextLevel", 100);

        if (currentLevel >= maxLevel)
        {
            currentLevel = maxLevel;
            currentExperience = maxExperienceAtMaxLevel;
            experienceForNextLevel = maxExperienceAtMaxLevel;
        }
    }

    public void ResetLevelData()
    {
        if (_character == null) return;

        PlayerPrefs.DeleteKey(_character.Data.Name + "_Group" + _currentSaveGroup + "_Level");
        PlayerPrefs.DeleteKey(_character.Data.Name + "_Group" + _currentSaveGroup + "_Experience");
        PlayerPrefs.DeleteKey(_character.Data.Name + "_Group" + _currentSaveGroup + "_ExperienceForNextLevel");
        PlayerPrefs.Save();

        currentLevel = 1;
        currentExperience = 0;
        experienceForNextLevel = 100;

        Debug.Log($"Сохраненные данные уровня и опыта для персонажа {_character.Data.Name} были сброшены.");
    }

    public void DisplayCurrentHeroLevelInfo()
    {
        if (_character == null) return;
        Debug.Log($"Character: {_character.Data.Name} | Level: {currentLevel} | Experience: {currentExperience}/{experienceForNextLevel}");
    }
}
