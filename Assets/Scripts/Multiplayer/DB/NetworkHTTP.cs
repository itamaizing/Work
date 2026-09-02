using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkHTTP : MonoBehaviour
{
    public static NetworkHTTP Instance { get; private set; }

    private Coroutine _postCoroutine;

    public static HeroData ConvertInHeroData(string data)
    {
        return JsonUtility.FromJson<HeroData>(data);
    }

    public static HeroesData ConvertInHeroesData(string data)
    {
        return JsonUtility.FromJson<HeroesData>(data);
    }

    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void Post(string uri, Dictionary<string, string> data, Action<string> success = null, Action<string> error = null)
    {
        _postCoroutine = StartCoroutine(PostJob(uri, data, success, error = null));
    }
    
    public void PostGetTalentData(Dictionary<string, string> data, Action<string> success, Action<string> error = null)
    {
        Post(URLLibrary.GetTalentData, data, success, error);
    }

    public void PostSetTalentData(string json, Action<string> success, Action<string> error = null)
    {
        _postCoroutine = StartCoroutine(PostJsonJob(URLLibrary.SetTalentData, json, success, error));
    }

    private IEnumerator PostJsonJob(string uri, string json, Action<string> success, Action<string> error = null)
    {
        var request = new UnityWebRequest(uri, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            error?.Invoke(request.error);
            Debug.LogError($"{request.error}");
        }
        else
        {
            success?.Invoke(request.downloadHandler.text);
            Debug.LogError($"{request.downloadHandler.text}");
        }
    }

    /// <summary>
    /// Gets the number of bottles by id
    /// </summary>
    /// <param name="data">the key is "id" for the user id</param>
    public void PostGetBottle(Dictionary<string, string> data, Action<string> success, Action<string> error = null)
    {
        Post(URLLibrary.GetBottle, data, success, error);
    }

    /// <summary>
    /// sets the number of bottles by id
    /// </summary>
    /// <param name="data">The key is "id" for the user ID and "bottle" for the number of bottles. </param>
    public void PostSetBottle(Dictionary<string, string> data, Action<string> success, Action<string> error = null)
    {
        Post(URLLibrary.SetBottle, data, success, error);
    }

    /// <summary>
    /// Set values for level, experience, skill points
    /// </summary>
    /// <param name="data">"id" for user ID. "heroName" for the hero search. "heroLVL" set lvl. "heroExp" set exp. "heroSkillPoints" set skill points.</param>
    /// <param name="success">Nothing is returned</param>
    public void PostSetHeroData(Dictionary<string, string> data, Action<string> success, Action<string> error = null)
    {
        Post(URLLibrary.SetHeroData, data, success, error);
    }

    /// <summary>
    /// Gets information about the hero (level, experience, number of skill points). After receiving it, you need to convert the JSON array to a C# class (use the public static HeroData ConvertInHeroData)
    /// </summary>
    /// <param name="data">the key is "id" for the user id, "heroName" for the hero search</param>
    public void PostGetHeroData(Dictionary<string, string> data, Action<string> success, Action<string> error = null)
    {
        Post(URLLibrary.GetHeroData, data, success, error);
    }
    
    public void PostSetTalentSingle(string json, Action<string> success, Action<string> error = null)
        => _postCoroutine = StartCoroutine(PostJsonJob(URLLibrary.SetTalentSingle, json, success, error));

    public void PostSetAttributePoint(string json, Action<string> success, Action<string> error = null)
        => _postCoroutine = StartCoroutine(PostJsonJob(URLLibrary.SetAttributePoint, json, success, error));

    /// <summary>
    /// Gets the saved ability-panel layout (skill-per-slot) for id+heroName.
    /// </summary>
    public void PostGetAbilityPanel(Dictionary<string, string> data, Action<string> success, Action<string> error = null)
    {
        Post(URLLibrary.GetAbilityPanel, data, success, error);
    }

    /// <summary>
    /// Saves the ability-panel layout. data keys: "id", "heroName", "layout" (JSON-serialized
    /// List&lt;SkillPanelSave&gt;).
    /// </summary>
    public void PostSetAbilityPanel(Dictionary<string, string> data, Action<string> success, Action<string> error = null)
    {
        Post(URLLibrary.SetAbilityPanel, data, success, error);
    }
    
    public void PostGetUserInfo(Dictionary<string, string> data, Action<string> success, Action<string> error = null)
    {
        Post(URLLibrary.GetUserInfo, data, success, error);
    }

    public void PostSaveMatchHistory(string json, Action<string> success, Action<string> error = null)
        => _postCoroutine = StartCoroutine(PostJsonJob(URLLibrary.SaveMatchHistory, json, success, error));

    public void PostGetMatchHistory(Dictionary<string, string> data, Action<string> success, Action<string> error = null)
    {
        Post(URLLibrary.GetMatchHistory, data, success, error);
    }
    
    private IEnumerator PostJob(string uri, Dictionary<string, string> data, Action<string> success, Action<string> error = null)
    {
        using (UnityWebRequest www = UnityWebRequest.Post(uri, data))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                error?.Invoke(www.error);
                //Debug.LogError($"Data send error : {www.downloadHandler.text}");
            }
            else
            {
                success?.Invoke(www.downloadHandler.text);
                //Debug.LogError($"Data send success : {www.downloadHandler.text}");
            }
        }
    }
}

[System.Serializable]
public class HeroData
{
    public int id;
    public int lvl;
    public int exp;
    public int skillpoints;
    public int users_id;
    public int heroes_id;
}

[System.Serializable]
public class HeroesData
{
    public HeroData[] Property1;
}