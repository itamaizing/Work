using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class User : NetworkBehaviour
{
    public static User Instance;

    public override void OnStartClient()
    {
        if (Instance == null && isOwned)
        {
            Instance = this;
            InitializeManagers();
        }
    }

    private void InitializeManagers()
    {
        if (LevelCharacterManager.Instance == null)
        {
            LevelCharacterManager.Initialize();
        }

        if (BottleUserManager.Instance == null)
        {
            BottleUserManager.Initialize();
        }
    }

    public class LevelCharacterManager
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

        private LevelCharacterManager() { }

        public static void Initialize()
        {
            if (_instance == null)
            {
                _instance = new LevelCharacterManager();
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

    public class BottleUserManager
    {
        private static BottleUserManager _instance;
        public static BottleUserManager Instance => _instance;

        private const int MaxBottles = 99;

        private int _currentBottles = 0;
        private float _currentBottleVolume = 0f;
        private string _currentUser;
        private string mainMenuSceneName = "MainMenu";

        private BottleUserManager() { }

        public static void Initialize()
        {
            if (_instance == null)
            {
                _instance = new BottleUserManager();
                _instance.LoadBottleData();
                SceneManager.sceneLoaded += _instance.OnSceneLoaded;
                Debug.Log("BottleUserManager initialized.");
            }
        }

        public static void Cleanup()
        {
            if (_instance != null)
            {
                SceneManager.sceneLoaded -= _instance.OnSceneLoaded;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == mainMenuSceneName)
            {
                LogBottleInfoOnClient();
            }
        }

        public void SetUser(string user)
        {
            _currentUser = user;
        }

        public void AddBottleVolume(float amount)
        {
            _currentBottleVolume += amount;

            if (_currentBottleVolume >= 1f)
            {
                _currentBottles = Mathf.Min(_currentBottles + 1, MaxBottles);
                _currentBottleVolume = 0f;
            }

            SaveBottleData();
        }

        public bool TryUseBottle()
        {
            if (_currentBottles > 0)
            {
                _currentBottles--;
                SaveBottleData();
                return true;
            }
            return false;
        }

        public int GetCurrentBottles() => _currentBottles;
        public float GetCurrentBottleVolume() => _currentBottleVolume;

        private void SaveBottleData()
        {
            PlayerPrefs.SetInt(_currentUser + "_Bottles", _currentBottles);
            PlayerPrefs.SetFloat(_currentUser + "_BottleVolume", _currentBottleVolume);
            PlayerPrefs.Save();
        }

        private void LoadBottleData()
        {
            _currentBottles = PlayerPrefs.GetInt(_currentUser + "_Bottles", 0);
            _currentBottleVolume = PlayerPrefs.GetFloat(_currentUser + "_BottleVolume", 0f);
        }

        public void LogBottleInfoOnClient()
        {
            Debug.Log($"Number of bottles: {_currentBottles}");
            Debug.Log($"The volume of the current bottle: {_currentBottleVolume * 100}%");
        }
    }
}
