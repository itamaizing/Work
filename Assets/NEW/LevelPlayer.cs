using UnityEngine;

public class LevelPlayer : MonoBehaviour
{
    private static LevelPlayer _instance;
    public static LevelPlayer Instance => _instance;

    private HeroComponent _character;
    private int _currentSaveGroup = 0;

    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience = 0;
    [SerializeField] private int experienceForNextLevel = 10;
    [SerializeField] private bool resetSaveData = false;

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

    private void Start()
    {
        // Проверка на необходимость сброса данных при старте
        if (resetSaveData)
        {
            ResetSaveData();
        }
    }

    public void SetHero(HeroComponent hero)
    {
        _character = hero;
        LoadLevelData();
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
        LogLevelInfo();
    }

    public void LogLevelInfo()
    {
        Debug.Log($"Персонаж: {_character.Data.Name}");
        Debug.Log($"Текущий уровень: {currentLevel}");
        Debug.Log($"Текущий опыт: {currentExperience}");
        Debug.Log($"Необходимый опыт для следующего уровня: {experienceForNextLevel}");
    }

    public void ResetSaveData()
    {
        if (_character != null)
        {
            PlayerPrefs.DeleteKey(_character.Data.Name + "_Group" + _currentSaveGroup + "_Level");
            PlayerPrefs.DeleteKey(_character.Data.Name + "_Group" + _currentSaveGroup + "_Experience");
            PlayerPrefs.DeleteKey(_character.Data.Name + "_Group" + _currentSaveGroup + "_ExperienceForNextLevel");

            currentLevel = 1;
            currentExperience = 0;
            experienceForNextLevel = 10;

            Debug.Log($"Данные сохранения для персонажа {_character.Data.Name} сброшены.");
        }
    }
}
