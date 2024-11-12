using UnityEngine;

public class LevelCharacterManager : MonoBehaviour
{
    private static LevelCharacterManager _instance;
    public static LevelCharacterManager Instance => _instance;

    private HeroComponent _character;
    private int _currentSaveGroup = 0;

    private int _currentLevel = 1;
    private int _currentExperience = 0;
    private int _experienceForNextLevel = 100;

    private const int _maxLevel = 9;
    private const int _maxExperienceAtMaxLevel = 800;

    public int MaxLevel => _maxLevel;

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
        if (_character == null || _currentLevel >= _maxLevel) return;

        _currentExperience += experience;
        CheckForLevelUp();
        SaveLevelData();
    }

    private void CheckForLevelUp()
    {
        while (_currentExperience >= _experienceForNextLevel && _currentLevel < _maxLevel)
        {
            _currentExperience -= _experienceForNextLevel;
            _currentLevel++;
            _experienceForNextLevel = CalculateExperienceForNextLevel();

            if (_currentLevel == _maxLevel)
            {
                _currentExperience = _maxExperienceAtMaxLevel;
                _experienceForNextLevel = _maxExperienceAtMaxLevel;
                break;
            }
        }
    }

    private int CalculateExperienceForNextLevel()
    {
        return _currentLevel * 100;
    }

    public int GetCurrentLevel() => _currentLevel;
    public int GetCurrentExperience() => _currentExperience;
    public int GetExperienceForNextLevel() => _experienceForNextLevel;

    private void SaveLevelData()
    {
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_Level", _currentLevel);
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_Experience", _currentExperience);
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_ExperienceForNextLevel", _experienceForNextLevel);
        PlayerPrefs.Save();
    }

    private void LoadLevelData()
    {
        _currentLevel = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_Level", 1);
        _currentExperience = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_Experience", 0);
        _experienceForNextLevel = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_ExperienceForNextLevel", 100);

        if (_currentLevel >= _maxLevel)
        {
            _currentLevel = _maxLevel;
            _currentExperience = _maxExperienceAtMaxLevel;
            _experienceForNextLevel = _maxExperienceAtMaxLevel;
        }
    }

    public void ResetLevelData()
    {
        if (_character == null) return;

        PlayerPrefs.DeleteKey(_character.Data.Name + "_Group" + _currentSaveGroup + "_Level");
        PlayerPrefs.DeleteKey(_character.Data.Name + "_Group" + _currentSaveGroup + "_Experience");
        PlayerPrefs.DeleteKey(_character.Data.Name + "_Group" + _currentSaveGroup + "_ExperienceForNextLevel");
        PlayerPrefs.Save();

        _currentLevel = 1;
        _currentExperience = 0;
        _experienceForNextLevel = 100;

        Debug.Log($"Сохраненные данные уровня и опыта для персонажа {_character.Data.Name} были сброшены.");
    }

    public void DisplayCurrentHeroLevelInfo()
    {
        if (_character == null) return;
        Debug.Log($"Character: {_character.Data.Name} | Level: {_currentLevel} | Experience: {_currentExperience}/{_experienceForNextLevel}");
    }
}
