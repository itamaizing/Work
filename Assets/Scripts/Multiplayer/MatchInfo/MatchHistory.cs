using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public struct MatchParticipantEntry
{
    public int userId;
    public string heroName;
}

[Serializable]
public class MatchHistorySaveRequest
{
    public string gamemode;
    public MatchParticipantEntry[] participants;
}

[Serializable]
public class MatchHistoryParticipantResponse
{
    public int users_id;
    public string nickname;
    public string heroName;
}

[Serializable]
public class MatchHistoryEntryResponse
{
    public int matchId;
    public string gamemode;
    public string playedAt;
    public MatchHistoryParticipantResponse[] participants;
}

[Serializable]
public class MatchHistoryListResponse
{
    public bool success;
    public MatchHistoryEntryResponse[] matches;
}

public static class MatchHistoryRepository
{
    public static void SaveMatch(GameMode gameMode, IEnumerable<MatchParticipantEntry> participants,
        Action<int> onSaved = null, Action onFailed = null)
    {
        var payload = new MatchHistorySaveRequest
        {
            gamemode = gameMode.ToString(),
            participants = new List<MatchParticipantEntry>(participants).ToArray()
        };
        string json = JsonConvert.SerializeObject(payload);

        NetworkHTTP.Instance.PostSaveMatchHistory(json,
            success: resp =>
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(resp);
                if (result == null || !(bool)result["success"])
                {
                    Debug.LogWarning("История матча не сохранена: " + resp);
                    onFailed?.Invoke();
                    return;
                }
                onSaved?.Invoke(Convert.ToInt32(result["matchId"]));
            },
            error: err =>
            {
                Debug.LogWarning("Ошибка сохранения истории матча: " + err);
                onFailed?.Invoke();
            });
    }

    public static void LoadHistory(int userId, Action<MatchHistoryEntryResponse[]> onLoaded, Action onFailed = null)
    {
        var data = new Dictionary<string, string> { { "id", userId.ToString() } };

        NetworkHTTP.Instance.PostGetMatchHistory(data,
            success: json =>
            {
                var result = JsonConvert.DeserializeObject<MatchHistoryListResponse>(json);
                if (result == null || !result.success)
                {
                    Debug.LogWarning("Не удалось получить историю матчей: " + json);
                    onFailed?.Invoke();
                    return;
                }
                onLoaded?.Invoke(result.matches);
            },
            error: err =>
            {
                Debug.LogWarning("Сервер недоступен (история матчей): " + err);
                onFailed?.Invoke();
            });
    }
}