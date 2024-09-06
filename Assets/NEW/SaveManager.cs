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

    public void SaveAttribute(int index, int points)
    {
        var currentPoints = _character.Data.Attributes.AttributeData.FirstOrDefault(o=>o.Id == index)!.Points;
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + _character.Data.Attributes.AttributeData.FirstOrDefault(o => o.Id == index)?.Name + "_Points", currentPoints + points);
        PlayerPrefs.Save();
        
        SaveAttributePoints(-points);
    }

    public void LoadAttributes()
    {
        foreach (var attribute in _character.Data.Attributes.AttributeData)
        {
            int savedPoints = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + 
                                                 attribute.Name + "_Points", 0);
            attribute.Points = savedPoints;
        }
    }
    
    public void LoadAttribute(int index)
    {
        var attribute = _character.Data.Attributes.AttributeData.FirstOrDefault(o => o.Id == index);
        
        if(attribute == null) return;
        
        int savedPoints = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + 
                                             attribute.Name + "_Points", 0);
        attribute.Points = savedPoints;
    }
    
    public void SaveTalent(int idGroup, int idTalent, bool isActive)
    {
        var isTalentActive = isActive ? 1 : 0;
        var talentGroup = _character.TalentManager.Talents.FirstOrDefault(o => o.ID == idGroup);
        var talent = talentGroup?.TalentsData.FirstOrDefault(o => o.Data.Id == idTalent);
        
        if(talentGroup == null || talent == null) return;
        
        var points = talentGroup.BonusAttributePoints();
        
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + talentGroup.Name + "_" + talent.Data.Name, isTalentActive);
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + talentGroup.Name + "_" + talent.Data.Name + "_Points_", points);
        PlayerPrefs.Save();

        if (isActive)
        {
            SaveAttributePoints(points);
        }
        else
        {
            SaveAttributePoints(-talent.Data.AttributePoints);   
        }
    }

    public void LoadTalent(int idGroup, int idTalent)
    {
        var groupTemp = _character.TalentManager.Talents.FirstOrDefault(o => o.ID == idGroup);
        var talentTemp = groupTemp?.TalentsData.FirstOrDefault(o=>o.Data.Id == idTalent);
        
        if(groupTemp == null || talentTemp == null) return;
                
        int isActive =  PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + groupTemp.Name + "_" + talentTemp.Data.Name, 0);
        int points = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + groupTemp.Name + "_" + talentTemp.Data.Name + "_Points_", 0);
        
        talentTemp.Data.IsOpen = isActive == 1;
        talentTemp.Data.AttributePoints = points;
    }

    public void LoadTalents()
    {
        foreach (var talentGroup in _character.TalentManager.Talents)
        {
            var groupTemp = _character.TalentManager.Talents.FirstOrDefault(o => o.ID == talentGroup.ID);
            
            if(groupTemp == null) return;
            
            foreach (var talent in talentGroup.TalentsData)
            {
                var talentTemp = groupTemp.TalentsData.FirstOrDefault(o=>o.Data.Id == talent.Data.Id);
        
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
            int savedPoints = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + 
                                                 attribute.Name + "_Points", 0);
            attribute.Points = savedPoints;
        }

        foreach (var talentGroup in _character.TalentManager.Talents)
        {
            var groupTemp = _character.TalentManager.Talents.FirstOrDefault(o => o.ID == talentGroup.ID);
            
            if(groupTemp == null) return;
            
            foreach (var talent in talentGroup.TalentsData)
            {
                var talentTemp = groupTemp.TalentsData.FirstOrDefault(o=>o.Data.Id == talent.Data.Id);
        
                if(talentTemp == null) return;
                
                int isActive =  PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + groupTemp.Name + "_" + talentTemp.Data.Name, 0);
                talent.Data.IsOpen = isActive == 1;
            }
        }
    }
}