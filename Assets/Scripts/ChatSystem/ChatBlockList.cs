using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public static class ChatBlockList
{
    private const string KeyPrefix = "chat.blocklist.";

    public static event Action Changed;

    private static Dictionary<int, string> _items = new Dictionary<int, string>();
    private static int _owner = int.MinValue;

    private static int CurrentOwner =>
        MPNetworkManager.Instance != null ? MPNetworkManager.Instance.UserID : -1;

    public static IReadOnlyDictionary<int, string> All
    {
        get { EnsureLoaded(); return _items; }
    }

    public static bool IsBlocked(int userId)
    {
        if (userId <= 0) return false;
        EnsureLoaded();
        return _items.ContainsKey(userId);
    }

    public static bool Add(int userId, string name)
    {
        if (userId <= 0) return false;
        EnsureLoaded();
        if (_items.ContainsKey(userId)) return false;

        _items[userId] = name ?? string.Empty;
        Save();
        Changed?.Invoke();
        return true;
    }

    public static bool Remove(int userId)
    {
        EnsureLoaded();
        if (!_items.Remove(userId)) return false;

        Save();
        Changed?.Invoke();
        return true;
    }

    public static bool TryFindByName(string name, out int userId)
    {
        EnsureLoaded();
        foreach (var pair in _items)
        {
            if (string.Equals(pair.Value, name, StringComparison.OrdinalIgnoreCase))
            {
                userId = pair.Key;
                return true;
            }
        }
        userId = 0;
        return false;
    }

    private static void EnsureLoaded()
    {
        int owner = CurrentOwner;
        if (owner == _owner) return;

        _owner = owner;
        _items = new Dictionary<int, string>();

        try
        {
            string json = PlayerPrefs.GetString(KeyPrefix + owner, string.Empty);
            if (!string.IsNullOrEmpty(json))
                _items = JsonConvert.DeserializeObject<Dictionary<int, string>>(json) ?? _items;
        }
        catch (Exception e)
        {
            Debug.LogWarning("ChatBlockList: не удалось прочитать список: " + e.Message);
        }
    }

    private static void Save()
    {
        PlayerPrefs.SetString(KeyPrefix + _owner, JsonConvert.SerializeObject(_items));
        PlayerPrefs.Save();
    }
}