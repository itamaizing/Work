using System;

public struct UserInfoData
{
    public string nickname;
    public int bottles;
}

public interface IUserInfoRepository
{
    void LoadUserInfo(string userKey, Action<UserInfoData> onLoaded, Action onFailed = null);
}