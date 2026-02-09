using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ISaveData
{
    void SaveInt(string key, int value);
    int LoadInt(string key, int defaultValue = 0);
    void SaveString(string key, string value);
    string LoadString(string key, string defaultValue = "");
}

public class PlayerPrefsSaveData : ISaveData
{
    public void SaveInt(string key, int value)
    {
       // Debug.Log("SAVED TALENTS  " + key + value);
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    public int LoadInt(string key, int defaultValue = 0)
    {
		//Debug.Log("Load TALENTS  " + key + defaultValue + " loaded: "+ PlayerPrefs.GetInt(key, defaultValue));
		return PlayerPrefs.GetInt(key, defaultValue);
    }

    public void SaveString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    public string LoadString(string key, string defaultValue = "")
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }
}

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance;

    private HeroComponent _character;
    private int _currentSaveGroup = 0;
    private ISaveData _saveData;
    //private AttributeSaveManager _attributeManager;
    private TalentSaveManager _talentManager;

    private AttributeSystem _attributeSystem;
    private SaveSystem _saveSystem = new SaveSystem();
    private AttributeSaveModifier _attributeModifier;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _saveData = new PlayerPrefsSaveData();
            _talentManager = new TalentSaveManager(_saveData, _instance);
            //_attributeManager = new AttributeSaveManager(_saveData);
            //_attributeModifier = new AttributeModifier(_attributeManager);
            //_attributeModifier = new AttributeSaveModifier(_attributeManager);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetHero(HeroComponent hero)
    {
        _character = hero;
        _attributeSystem = hero.AttributeSystem;
        LoadHeroData();
    }

    public void SetSaveIndex(int index)
    {
        _currentSaveGroup = index;
        LoadHeroData();
    }
    
    public void SaveAttributePoints(int points)
    {
        //_saveSystem.Save($"{_character.Data.Name}_Group{_currentSaveGroup}_FreeAttributesPoints", _attributeSystem.Points);
    }
    
    public int LoadAttributePoints()
    {
        int points = 0;
        _saveSystem.Load<int>($"{_character.Data.Name}_Group{_currentSaveGroup}_FreeAttributesPoints", e => points = e);
        return points;
    }

    public void AddAttributesModif(Attribute attribute, AttributeModifier modif)
    {
        attribute.AddModifier(modif);
        SaveAttribute(attribute);
    }

    public void RemoveAttributesModif(Attribute attribute, AttributeModifier modif)
    {
        attribute.RemoveModifier(modif);
        SaveAttribute(attribute);
    }

    public void SaveAttribute(Attribute attribute)
    {
       // _saveSystem.Save($"{_character.Data.Name}_Group{_currentSaveGroup}_{attribute.Name}_Points", attribute.Modifiers);
    }

    public List<AttributeModifier> LoadAttribute(Attribute attribute)
    {
        List<AttributeModifier> modifs = new();
        //_saveSystem.Load<List<AttributeModifier>>($"{_character.Data.Name}_Group{_currentSaveGroup}_{attribute.Name}_Points", e => modifs = e);

       /* Attribute atrib = _attributeSystem.Attribute.FirstOrDefault(a => a.Name == attribute.Name);
        if (atrib != null)
        {
            foreach (AttributeModifier modif in modifs)
            {
                atrib.AddModifier(modif);
            }
        }
        Debug.Log(modifs.Count);*/

        return modifs;
    }

    public void SaveTalent(int idGroup, int row, string idTalent, bool isActive)
    {
        _talentManager.SaveTalent(_character, idGroup, row, idTalent, isActive, _currentSaveGroup);
    }

    public void LoadTalent(int idGroup, int row, string idTalent, bool needActivate)
    {
        _talentManager.LoadTalent(_character, idGroup, row, idTalent, needActivate, _currentSaveGroup);
    }

    public int ReduceFreePoints(int pointsToDeduct)
    {
        return 0; //_attributeModifier.ReduceFreePoints(_character, pointsToDeduct, _currentSaveGroup);
    }

    public void ReduceAttributePoints(int pointsToDeduct)
    {
        //_attributeModifier.ReduceAttributePoints(_character, pointsToDeduct, _currentSaveGroup);
    }

    public void SaveAllData()
    {
        //_attributeManager.SaveAllAttributes(_character, _currentSaveGroup);
        _talentManager.SaveAllTalents(_character, _currentSaveGroup);
    }

    public void LoadAllData()
    {
        //_attributeManager.LoadAllAttributes(_character, _currentSaveGroup);
        _talentManager.LoadAllTalents(_character, _currentSaveGroup);
    }

    private void LoadHeroData()
    {
        LoadAllData();
    }
}