using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

public class SaveSystem
{
    public void Save(string key, object data, Action<bool> callback = null)
    {
        string path = BuildPath(key);
        string json = JsonConvert.SerializeObject(data);
        using (var filestream = new StreamWriter(path))
        {
            filestream.WriteLine(json);
        }

        callback?.Invoke(true);
    }

    public void Load<T>(string key, Action<T> callback)
    {
        string path = BuildPath(key);
        if (File.Exists(path))
        {
            using (var filestream = new StreamReader(path))
            {
                var json = filestream.ReadToEnd();
                var data = JsonConvert.DeserializeObject<T>(json);

                callback?.Invoke(data);
            }
        }
    }

    public T LoadOrDefault<T>(string key, T defaultValue = default)
    {
        string path = BuildPath(key);
        if (!File.Exists(path))
        {
            return defaultValue;
        }

        try
        {
            using (var filestream = new StreamReader(path))
            {
                var json = filestream.ReadToEnd();
                if (string.IsNullOrWhiteSpace(json)) return defaultValue;

                var data = JsonConvert.DeserializeObject<T>(json);
                return data != null ? data : defaultValue;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Ошибка чтения ключа {key}: {ex.Message}");
            return defaultValue;
        }
    }

    private string BuildPath(string key)
    {
        return Path.Combine(Application.persistentDataPath, key);
    }
}