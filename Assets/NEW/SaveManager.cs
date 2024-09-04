using System.Linq;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance;

    private Character _character;
    private int _currentSaveGroup = 0;
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
    }

    public void SetHero(Character hero)
    {
        _character = hero;
        LoadData();
    }

    public void SetSaveIndex(int index)
    {
        _currentSaveGroup = index;
        LoadData();
    }

    public void SaveData()
    {
        PlayerPrefs.SetInt(_character.Data.Name, _character.Data.ID);

        foreach (var t in _character.Data.Attributes.AttributeData)
        {
            PlayerPrefs.SetInt(_character.Data.Name + t.Name + "_Points", t.Points);
        }

        PlayerPrefs.Save();
    }

    public void AddAttribute(int index, int points)
    {
        var currentPoints = GetAttributeValue(index);
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + _character.Data.Attributes.AttributeData.FirstOrDefault(o => o.Id == index)?.Name + "_Points", currentPoints + points);
        PlayerPrefs.Save();
    }
    
    public int GetAttributeValue(int index)
    {
       return PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + _character.Data.Attributes.AttributeData.FirstOrDefault(o => o.Id == index)?.Name + "_Points", 0);
    }
    
    public void SaveTalent(int indexGroup, int index, bool isActive)
    {
        var points = isActive ? 1 : 0;
        PlayerPrefs.SetInt(_character.Data.Name + _character.Data.Talents[indexGroup].TalentsData[index].Name, points);
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        foreach (var t in _character.Data.Attributes.AttributeData)
        {
            int savedPoints = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + t.Name + "_Points", 0);
            t.Points = savedPoints;
        }
    }
}