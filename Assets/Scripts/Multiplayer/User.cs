using Mirror;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class User : NetworkBehaviour
{
    public static User Instance;

    private int _id = -37;

    public int Id { get => _id; }

    public void SetID(int id)
    {
        if (_id < 0)
        {
            _id = id;

            SaveManager.Instance.LoadBottles(_id.ToString(),
                onLoaded: (bottles, volume) => BottleUserManager.Instance.ApplyLoadedBottleData(bottles, volume),
                onFailed: null);
        }
    }

    public override void OnStartClient()
    {
        if (isLocalPlayer && isOwned)
        {
            if (Instance == null)
            {
                Instance = this;
                _id = MPNetworkManager.Instance.UserID;
                InitializeManagers();

                AddPlayer(ServerManager.Instance.CurrentHeroIndex);
            }
            else if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
        }
    }

    [Command(requiresAuthority = false)]
    private void AddPlayer(int characterIndex)
    {
        GameObject player = Instantiate(MPNetworkManager.Instance.HeroList[characterIndex].gameObject);
        NetworkServer.Spawn(player, connectionToClient);
        MPNetworkManager.Instance.AddPlayer(player);
        RpcAddPlayer(player);
    }

    [ClientRpc]
    private void RpcAddPlayer(GameObject player)
    {
        MPNetworkManager.Instance.Players.Add(player);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            BottleUserManager.Instance?.Dispose();
            LevelCharacterManager.Instance?.Dispose();
            Instance = null;
        }
    }

    private void InitializeManagers()
    {
        BottleUserManager.Instance?.SetUser(_id.ToString());
        BottleUserManager.Instance?.BottleInitialize();
        LevelCharacterManager.Instance?.LevelInitialize();
    }
}

public class BottleUserManager
{
    private static BottleUserManager _instance;
    public static BottleUserManager Instance => _instance ??= new BottleUserManager();

    private const int MaxBottles = 99;

    private int _currentBottles = 0;
    private float _currentBottleVolume = 0f;
    private string _currentUser;

    public event Action<int> OnBottlesChanged;

    public void SetUser(string user)
    {
        _currentUser = user;
        Debug.Log($"Current user set to: {_currentUser}");
    }

    public void BottleInitialize()
    {
        _instance = this;

        if (!string.IsNullOrEmpty(_currentUser))
            SaveManager.Instance.LoadBottles(_currentUser, ApplyLoadedBottleData, onFailed: null);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void Dispose()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ApplyLoadedBottleData(int bottles, float volume)
    {
        _currentBottles = bottles;
        _currentBottleVolume = volume;
        LogBottleInfoOnClient();
        OnBottlesChanged?.Invoke(_currentBottles);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LogBottleInfoOnClient();
    }

    public void ResetBottleData()
    {
        if (string.IsNullOrEmpty(_currentUser)) return;

        int previousBottles = _currentBottles;
        _currentBottles = 0;
        _currentBottleVolume = 0f;

        Persist(rollbackTo: previousBottles);
        OnBottlesChanged?.Invoke(_currentBottles);
    }

    public void AddBottleVolume(float amount)
    {
        int previousBottles = _currentBottles;
        _currentBottleVolume += amount;

        if (_currentBottleVolume >= 1f)
        {
            _currentBottles = Mathf.Min(_currentBottles + 1, MaxBottles);
            _currentBottleVolume = 0f;
        }

        Persist(rollbackTo: previousBottles);
        OnBottlesChanged?.Invoke(_currentBottles);
    }


    public bool TryUseBottle()
    {
        if (_currentBottles <= 0) return false;

        int previousBottles = _currentBottles;
        _currentBottles--;
        Persist(rollbackTo: previousBottles);
        return true;
    }

    public int GetCurrentBottles() => _currentBottles;
    public float GetCurrentBottleVolume() => _currentBottleVolume;

    private void Persist(int rollbackTo)
    {
        if (string.IsNullOrEmpty(_currentUser))
        {
            Debug.LogWarning("Cannot save bottle data: User not set.");
            return;
        }

        SaveManager.Instance.SaveBottles(_currentUser, _currentBottles, _currentBottleVolume,
            onSaved: confirmedBottles =>
            {
                if (confirmedBottles == _currentBottles) return;
                _currentBottles = confirmedBottles;
                OnBottlesChanged?.Invoke(_currentBottles);
            },
            onFailed: () =>
            {
                _currentBottles = rollbackTo;
                Debug.LogWarning("[BottleUserManager] Сохранение бутылок не удалось, откат до " + _currentBottles);
                OnBottlesChanged?.Invoke(_currentBottles);
            });
    }

    public void LogBottleInfoOnClient()
    {
        Debug.Log($"Number of bottles: {_currentBottles}");
        Debug.Log($"The volume of the current bottle: {_currentBottleVolume * 100}%");
    }
}

public class LevelCharacterManager
{
    private static LevelCharacterManager _instance;
    public static LevelCharacterManager Instance => _instance ??= new LevelCharacterManager();

    private HeroComponent _character;
    private int _currentSaveGroup = 0;

    private int _currentLevel = 1;
    private int _currentExperience = 0;
    private int _experienceForNextLevel = 100;

    private const int _maxLevel = 9;
    private const int _maxExperienceAtMaxLevel = 800;

    public event Action<int> OnLevelChanged;
    public event Action<int, int> OnExperienceChanged;

    public int MaxLevel => _maxLevel;

    public void LevelInitialize()
    {
        if (_instance == null) _instance = this;
    }

    public void Dispose() { }
    
    public void ApplyLoadedLevelData(HeroComponent hero, int level, int experience)
    {
        _character = hero;

        _currentLevel = Mathf.Clamp(level, 1, _maxLevel);
        _currentExperience = experience;
        _experienceForNextLevel = CalculateExperienceForNextLevel();

        if (_currentLevel >= _maxLevel)
        {
            _currentLevel = _maxLevel;
            _currentExperience = _maxExperienceAtMaxLevel;
            _experienceForNextLevel = _maxExperienceAtMaxLevel;
        }

        OnLevelChanged?.Invoke(_currentLevel);
        OnExperienceChanged?.Invoke(_currentExperience, _experienceForNextLevel);
        DisplayCurrentHeroLevelInfo();
    }

    public bool TryGetCurrentHero(out HeroComponent hero)
    {
        hero = _character;
        return hero != null;
    }
    
    public void SetHero(HeroComponent hero)
    {
        _character = hero;
    }

    public HeroComponent GetHero() => _character;

    public void SetSaveIndex(int index)
    {
        _currentSaveGroup = index;
    }

    public void AddExperience(int experience)
    {
        if (_character == null || _currentLevel >= _maxLevel) return;

        _currentExperience += experience;
        CheckForLevelUp();

        int skillPoints = _currentLevel - _character.TalentManager.ActiveTalents.Count;
        int attributePoints = _currentLevel - _character.AttributeSystem.GetSpentPoints();

        SaveManager.Instance.SaveHeroLevel(_currentLevel, _currentExperience, skillPoints, attributePoints);

        OnExperienceChanged?.Invoke(_currentExperience, _experienceForNextLevel);
    }

    public void ResetAllLevelData()
    {
        if (_character == null) return;

        string baseKey = _character.Data.Name + "_Group" + _currentSaveGroup;

        PlayerPrefs.DeleteKey(baseKey + "_Level");
        PlayerPrefs.DeleteKey(baseKey + "_Experience");
        PlayerPrefs.DeleteKey(baseKey + "_ExperienceForNextLevel");
        PlayerPrefs.Save();

        _currentLevel = 1;
        _currentExperience = 0;
        _experienceForNextLevel = 100;

        OnLevelChanged?.Invoke(_currentLevel);
        OnExperienceChanged?.Invoke(_currentExperience, _experienceForNextLevel);
    }

    public void LevelChanged() => OnLevelChanged?.Invoke(_currentLevel);
    
    public void PreloadHeroLevelData(HeroComponent hero, int saveIndex = 0)
    {
        string key = hero.Data.Name + "_Group" + saveIndex;

        int level = PlayerPrefs.GetInt(key + "_Level", 1);
        int exp = PlayerPrefs.GetInt(key + "_Experience", 0);
        int maxExp = PlayerPrefs.GetInt(key + "_ExperienceForNextLevel", 100);

        if (level >= _maxLevel)
        {
            level = _maxLevel;
            exp = _maxExperienceAtMaxLevel;
            maxExp = _maxExperienceAtMaxLevel;
        }

        _currentLevel = level;
        _currentExperience = exp;
        _experienceForNextLevel = maxExp;
    }

    private void CheckForLevelUp()
    {
        while (_currentExperience >= _experienceForNextLevel && _currentLevel < _maxLevel)
        {
            _currentExperience -= _experienceForNextLevel;
            _currentLevel++;
            _experienceForNextLevel = CalculateExperienceForNextLevel();

            LevelChanged();

            if (_currentLevel == _maxLevel)
            {
                _currentExperience = _maxExperienceAtMaxLevel;
                _experienceForNextLevel = _maxExperienceAtMaxLevel;
                break;
            }
        }
    }

    private int CalculateExperienceForNextLevel() => _currentLevel * 100;

    public int GetCurrentLevel() => _currentLevel;
    public int GetCurrentExperience() => _currentExperience;
    public int GetExperienceForNextLevel() => _experienceForNextLevel;

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
    }

    public void DisplayCurrentHeroLevelInfo()
    {
        if (_character == null) return;
        Debug.Log($"Character: {_character.Data.Name} | Level: {_currentLevel} | Experience: {_currentExperience}/{_experienceForNextLevel}");
    }
}