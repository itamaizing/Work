using System.Linq;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance;

    private HeroComponent _character;
    private int _currentSaveGroup = 0;
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
    }

    public void SetHero(HeroComponent hero)
    {
        _character = hero;
        LoadHeroData();
    }

    public void SetSaveIndex(int index)
    {
        _currentSaveGroup = index;
        LoadHeroData();
    }
    
    public void SaveAttributePoints(int points)
    {
        var currentPoints = _character.Data.Attributes.FreeAttributePointsCount;
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup  + "_FreeAttributesPoints", currentPoints + points);
        PlayerPrefs.Save();
    }
    
    public void LoadAttributePoints()
    {
        var points =  PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_FreeAttributesPoints", 0);
        _character.Data.Attributes.FreeAttributePointsCount = points;
    }

    public int GetAttributePoints()
    {
        var points =  PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_FreeAttributesPoints", 0);
        return points;
    }
    

    public void SaveAttribute(int index, int points)
    {
        if(GetAttributePoints() - points < 0) return;
        
        var currentPoints = _character.Data.Attributes.AttributeData.FirstOrDefault(o=>o.Id == index)!.Points;
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + _character.Data.Attributes.AttributeData.FirstOrDefault(o => o.Id == index)?.Name + "_Points", currentPoints + points);
        PlayerPrefs.Save();
        
        SaveAttributePoints(-points);
    }

    public void LoadAttributes()
    {
        foreach (var attribute in _character.Data.Attributes.AttributeData)
        {
            var savedPoints = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + attribute.Name + "_Points", 0);
            attribute.Points = savedPoints;
        }
    }
    
    public void LoadAttribute(int index)
    {
        var attribute = _character.Data.Attributes.AttributeData.FirstOrDefault(o => o.Id == index);
        
        if(attribute == null) return;
        
        var savedPoints = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + attribute.Name + "_Points", 0);
        attribute.Points = savedPoints;
    }
    
    public void SaveTalent(int idGroup, string idTalent, bool isActive)
    {
        var isTalentActive = isActive ? 1 : 0;
        var talentGroup = _character.TalentManager.Talents.FirstOrDefault(o => o.ID == idGroup);
        var talent = talentGroup?.TalentsData.FirstOrDefault(o => o.Data.Name == idTalent);
        
        if(talentGroup == null || talent == null) return;
        
        var points = talentGroup.BonusAttributePoints(talent.Data.Name, !isActive);
        
        if(GetAttributePoints() + points < 0) return;
        
        SaveAttributePoints(points);
        
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + talentGroup.Name + "_" + talent.Data.Name, isTalentActive);
        PlayerPrefs.Save();
    }

    public void LoadTalent(int idGroup, string idTalent)
    {
        var groupTemp = _character.TalentManager.Talents.FirstOrDefault(o => o.ID == idGroup);
        var talentTemp = groupTemp?.TalentsData.FirstOrDefault(o=>o.Data.Name == idTalent);
        
        if(groupTemp == null || talentTemp == null) return;
                
        var isActive =  PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + groupTemp.Name + "_" + talentTemp.Data.Name, 0);
        
        talentTemp.Data.IsOpen = isActive == 1;
    }

    public void LoadTalents()
    {
        foreach (var talentGroup in _character.TalentManager.Talents)
        {
            var groupTemp = _character.TalentManager.Talents.FirstOrDefault(o => o.ID == talentGroup.ID);
            
            if(groupTemp == null) return;
            
            foreach (var talent in talentGroup.TalentsData)
            {
                var talentTemp = groupTemp.TalentsData.FirstOrDefault(o=>o.Data.Name == talent.Data.Name);
        
                if(talentTemp == null) return;
                
                int isActive =  PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + groupTemp.Name + "_" + talentTemp.Data.Name, 0);
                talent.Data.IsOpen = isActive == 1;
            }
        }
    }

    private void LoadHeroData()
    {
        foreach (var attribute in _character.Data.Attributes.AttributeData)
        {
            var savedPoints = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + attribute.Name + "_Points", 0);
            attribute.Points = savedPoints;
        }

        foreach (var talentGroup in _character.TalentManager.Talents)
        {
            var groupTemp = _character.TalentManager.Talents.FirstOrDefault(o => o.ID == talentGroup.ID);
            
            if(groupTemp == null) return;
            
            foreach (var talent in talentGroup.TalentsData)
            {
                var talentTemp = groupTemp.TalentsData.FirstOrDefault(o=>o.Data.Name == talent.Data.Name);
        
                if(talentTemp == null) return;
                
                var isActive =  PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + groupTemp.Name + "_" + talentTemp.Data.Name, 0);
                talent.Data.IsOpen = isActive == 1;
            }
        }
    }
}