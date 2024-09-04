using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance;
    
    private string saveFilePath;
    private CharacterData currentHero;

    void Awake()
    {
        if (_instance == null) 
        {
            _instance = this;
        } 
        
        saveFilePath = Path.Combine(Application.persistentDataPath, "saveData.json");
    }
    
    public CharacterData SelectHero(int heroId)
    {
        currentHero = LoadHero(heroId);
        return currentHero;
    }
    
    public void AddHeroToSave(HeroComponent hero)
    {
        CharacterData newHeroData = hero.Data;
        List<CharacterData> heroes = LoadData();
        heroes.Add(newHeroData);
        SaveData(heroes);
    }

    public void SaveData(List<CharacterData> heroes)
    {
        string json = JsonUtility.ToJson(new SaveData(heroes), true);
        File.WriteAllText(saveFilePath, json);
    }
    
    public void SaveCurrentHeroData()
    {
        if (currentHero != null)
        {
            List<CharacterData> heroes = LoadData();
            
            int index = heroes.FindIndex(h => h.ID == currentHero.ID);
            if (index >= 0)
            {
                heroes[index] = currentHero;
            }
            else
            {
                heroes.Add(currentHero);
            }
            SaveData(heroes);
        }
        else
        {
            Debug.LogWarning("No hero selected to save.");
        }
    }

    private CharacterData LoadHero(int heroId)
    {
        List<CharacterData> heroes = LoadData();
        
        return heroes.Find(h => h.ID == heroId);
    }
    
    public List<CharacterData> LoadData()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);
        
            if (saveData == null)
            {
                Debug.LogError("Failed to load save data. Returning default heroes.");
                return GetDefaultHeroes();
            }

            if (saveData.Heroes == null)
            {
                Debug.LogWarning("Loaded hero list is null. Returning default heroes.");
                return GetDefaultHeroes();
            }

            if (saveData.Heroes.Count == 0)
            {
                Debug.LogWarning("Loaded hero list is empty. Returning default heroes.");
                return GetDefaultHeroes();
            }

            return saveData.Heroes;
        }
        else
        {
            return GetDefaultHeroes();
        }
    }

    private List<CharacterData> GetDefaultHeroes()
    {
        List<CharacterData> defaultHeroes = new List<CharacterData>();
        
        CharacterData hero = ScriptableObject.CreateInstance<CharacterData>();
        
        //hero.SetToDefault();

        defaultHeroes.Add(hero);

        return defaultHeroes;
    }
}

[Serializable]
public class SaveData
{
    public List<CharacterData> Heroes;

    public SaveData(List<CharacterData> heroes)
    {
        Heroes = heroes;
    }
}