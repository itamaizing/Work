using System;

public class LocalUserInfoRepository : IUserInfoRepository
{
    private readonly ISaveData _saveData;

    public LocalUserInfoRepository(ISaveData saveData)
    {
        _saveData = saveData;
    }

    public void LoadUserInfo(string userKey, Action<UserInfoData> onLoaded, Action onFailed = null)
    {
        string nickname = _saveData.LoadString($"{userKey}_Nickname", "Player");
        int bottles = _saveData.LoadInt($"{userKey}_Bottles", 0);

        onLoaded?.Invoke(new UserInfoData { nickname = nickname, bottles = bottles });
    }
}