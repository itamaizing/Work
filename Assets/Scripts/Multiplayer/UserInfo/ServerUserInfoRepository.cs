using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class ServerUserInfoRepository : IUserInfoRepository
{
    [Serializable]
    private class UserInfoResponse
    {
        public bool success;
        public string nickname;
        public int bottles;
    }

    public void LoadUserInfo(string userKey, Action<UserInfoData> onLoaded, Action onFailed = null)
    {
        var data = new Dictionary<string, string> { { "id", userKey } };

        NetworkHTTP.Instance.PostGetUserInfo(data,
            success: json =>
            {
                var result = JsonConvert.DeserializeObject<UserInfoResponse>(json);
                if (result == null || !result.success)
                {
                    Debug.LogWarning("Не удалось получить данные пользователя: " + json);
                    onFailed?.Invoke();
                    return;
                }

                onLoaded?.Invoke(new UserInfoData { nickname = result.nickname, bottles = result.bottles });
            },
            error: err =>
            {
                Debug.LogWarning($"[ServerUserInfoRepository] Сервер недоступен (инфо пользователя): {err}");
                onFailed?.Invoke();
            });
    }
}