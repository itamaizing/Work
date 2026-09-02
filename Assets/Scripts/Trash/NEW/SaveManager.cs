using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ISaveData
{
    void SaveInt(string key, int value);
    int LoadInt(string key, int defaultValue = 0);
    void SaveString(string key, string value);
    string LoadString(string key, string defaultValue = "");
    void SaveFloat(string key, float value);
    float LoadFloat(string key, float defaultValue = 0f);
}

public class PlayerPrefsSaveData : ISaveData
{
    public void SaveInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    public int LoadInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);

    public void SaveString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    public string LoadString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);

    public void SaveFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    public float LoadFloat(string key, float defaultValue = 0f) => PlayerPrefs.GetFloat(key, defaultValue);
}

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance;

    public int CurrentSaveGroup => _currentSaveGroup;

    private HeroComponent _character;
    private int _currentSaveGroup = 0;
    private ISaveData _saveData;
    private readonly SaveSystem _saveSystem = new SaveSystem();

    private IHeroProgressRepository _repository;
    private IUserInfoRepository _userInfoRepository;
    
    private int _pendingRequestsCount;
    public bool HasPendingRequests => _pendingRequestsCount > 0;
    public event Action<bool> PendingStateChanged;
    
    private readonly Dictionary<string, bool> _talentRequestInFlight = new();
    private readonly Dictionary<string, (bool isActive, int lvl)> _talentPendingState = new();
    
    private static string TalentKey(int idGroup, int row, string idTalent) => $"{idGroup}_{row}_{idTalent}";

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _saveData = new PlayerPrefsSaveData();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private bool UseServerPersistence => MPNetworkManager.Instance != null && MPNetworkManager.Instance.UserID > 0;
    
    public void Initialize()
    {
        if (_repository == null)
        {
            _repository = UseServerPersistence
                ? new ServerHeroProgressRepository()
                : new LocalHeroProgressRepository(_saveData, _saveSystem);
        }

        if (_userInfoRepository == null)
        {
            _userInfoRepository = UseServerPersistence
                ? new ServerUserInfoRepository()
                : new LocalUserInfoRepository(_saveData);
        }
    }

    public void SetHero(HeroComponent hero)
    {
        Initialize();
        _character = hero;
    }

    public void SetSaveIndex(int index)
    {
        _currentSaveGroup = index;
    }
    
    public void LoadHeroProgress(UIMenuMainAttributesPanel attributesPanel, Func<bool> isStillCurrent, Action onComplete)
    {
        if (!EnsureRepository()) { onComplete?.Invoke(); return; }
        _repository.Load(_character, attributesPanel, _currentSaveGroup, isStillCurrent, onComplete);
    }
    
    public void LoadUserInfo(string userKey, Action<UserInfoData> onLoaded, Action onFailed = null)
    {
        Initialize();
        _userInfoRepository.LoadUserInfo(userKey, onLoaded, onFailed);
    }
    
    public void LoadTalentPage(HeroComponent hero,
        Action<TalentSnapshotEntry[], AttributeSnapshotEntry[]> onLoaded, Action onFailed = null)
    {
        if (!EnsureRepository()) { onFailed?.Invoke(); return; }
        _repository.LoadTalentPage(hero, _currentSaveGroup, onLoaded, onFailed);
    }
    
    private void IncrementPending()
    {
        _pendingRequestsCount++;
        PendingStateChanged?.Invoke(HasPendingRequests);
    }

    private void DecrementPending()
    {
        _pendingRequestsCount = Mathf.Max(0, _pendingRequestsCount - 1);
        PendingStateChanged?.Invoke(HasPendingRequests);
    }

    public void SaveTalent(int idGroup, int row, string idTalent, bool isActive, int lvl)
    {
        if (!EnsureRepository()) return;

        var group = _character.TalentManager.TalentsGroups.FirstOrDefault(g => g.ID == idGroup);
        var talent = group?.TalentRows[row].Talents?.FirstOrDefault(t => t.Data.Name == idTalent);
        if (group == null || talent == null) return;
        if (isActive && !_character.TalentManager.CanOpenTalent) return;

        talent.Data.SetOpen(isActive);
        talent.Data.SetLevel(lvl);
        _character.TalentManager.SetActive(idGroup, row, idTalent, isActive);

        string key = TalentKey(idGroup, row, idTalent);

        if (_talentRequestInFlight.TryGetValue(key, out bool inFlight) && inFlight)
        {
            _talentPendingState[key] = (isActive, lvl);
            return;
        }

        SendTalentRequest(idGroup, row, idTalent, isActive, lvl, key);
    }
    
    private void SendTalentRequest(int idGroup, int row, string idTalent, bool isActive, int lvl, string key)
    {
        var group = _character.TalentManager.TalentsGroups.FirstOrDefault(g => g.ID == idGroup);
        var talent = group?.TalentRows[row].Talents?.FirstOrDefault(t => t.Data.Name == idTalent);
        if (group == null || talent == null) return;

        bool prevOpen = talent.Data.IsOpen;
        int prevLvl = talent.Data.Level;

        _talentRequestInFlight[key] = true;
        IncrementPending();

        _repository.SaveTalent(_character, idGroup, row, idTalent, isActive, lvl, _currentSaveGroup,
            onFreeTalentPointsChanged: pts =>
            {
                _character.TalentManager.SetPoints(pts);
                OnTalentRequestFinished(idGroup, row, idTalent, key);
            },
            onFailed: () =>
            {
                talent.Data.SetOpen(prevOpen);
                talent.Data.SetLevel(prevLvl);
                _character.TalentManager.SetActive(idGroup, row, idTalent, prevOpen);
                OnTalentRequestFinished(idGroup, row, idTalent, key);
            });
    }

    private void OnTalentRequestFinished(int idGroup, int row, string idTalent, string key)
    {
        _talentRequestInFlight[key] = false;

        if (_talentPendingState.TryGetValue(key, out var pending))
        {
            _talentPendingState.Remove(key);
            SendTalentRequest(idGroup, row, idTalent, pending.isActive, pending.lvl, key);
        }
        DecrementPending();
    }

    public void SaveAttributePoint(Attribute attribute, int delta)
    {
        if (!EnsureRepository()) return;

        IncrementPending();
        _repository.SaveAttributePoint(_character, attribute.Name, delta, _currentSaveGroup,
            onFreeAttributePointsChanged: pts => DecrementPending(),
            onFailed: () => DecrementPending());
    }

    public void SaveHeroLevel(int level, int experience, int skillPoints, int attributePoints)
    {
        if (!EnsureRepository()) return;
        _repository.SaveLevel(_character, _currentSaveGroup, level, experience, skillPoints, attributePoints);
    }
    
    public void SaveAbilityLayout(string heroName, List<SkillPanelSave> layout,
        Action<List<SkillPanelSave>> onSaved = null, Action onFailed = null)
    {
        Initialize();
        _repository.SaveAbilityLayout(heroName, _currentSaveGroup, layout, onSaved, onFailed);
    }

    public void LoadAbilityLayout(string heroName, Action<List<SkillPanelSave>> onLoaded, Action onFailed = null)
    {
        Initialize();
        _repository.LoadAbilityLayout(heroName, _currentSaveGroup, onLoaded, onFailed);
    }
    
    public void SaveTalentPage(HeroComponent hero, TalentSnapshotEntry[] talents,
        AttributeSnapshotEntry[] attributes, Action onSaved = null, Action onFailed = null)
    {
        if (!EnsureRepository()) { onFailed?.Invoke(); return; }

        if (HasPendingRequests)
        {
            void WaitAndSave(bool hasPending)
            {
                if (!hasPending)
                {
                    PendingStateChanged -= WaitAndSave;
                    DoSave();
                }
            }
            PendingStateChanged += WaitAndSave;
            return;
        }

        DoSave();

        void DoSave()
        {
            Debug.LogError($"Count talents: {talents.Length}");
            _repository.SaveTalentPage(hero, _currentSaveGroup, talents, attributes, onSaved, onFailed);
        }
    }

    public void SaveBottles(string userKey, int bottles, float bottleVolume, Action<int> onSaved, Action onFailed)
    {
        Initialize();
        _repository.SaveBottles(userKey, bottles, bottleVolume, onSaved, onFailed);
    }

    public void LoadBottles(string userKey, Action<int, float> onLoaded, Action onFailed = null)
    {
        Initialize();
        _repository.LoadBottles(userKey, onLoaded, onFailed);
    }

    private bool EnsureRepository()
    {
        Initialize();
        return _repository != null;
    }
}