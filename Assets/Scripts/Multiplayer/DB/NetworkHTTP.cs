using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkHTTP : MonoBehaviour
{
    public static NetworkHTTP Instance { get; private set; }

    private Coroutine _authorizationCoroutine;

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

    public void Post(string uri, Dictionary<string, string> data, Action<string> success, Action<string> error = null)
    {
        _authorizationCoroutine = StartCoroutine(PostJob(uri, data, success, error = null));
    }

    private IEnumerator PostJob(string uri, Dictionary<string, string> data, Action<string> success, Action<string> error = null)
    {
        using (UnityWebRequest www = UnityWebRequest.Post(uri, data))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                error?.Invoke(www.error);
            else
                success?.Invoke(www.downloadHandler.text);
        }
    }
}
