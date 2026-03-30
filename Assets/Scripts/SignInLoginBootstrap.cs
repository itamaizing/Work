using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SignInLoginBootstrap : MonoBehaviour
{
    [SerializeField] private Authorization _authorization;
    [SerializeField] private SceneAsset _menuScene;
    [SerializeField] private SceneAsset _gameScene;
    [SerializeField] private WebSocketClient _webSocketClient;

    private int _id;

    private void OnEnable()
    {
        _authorization.Successed += OnSuccessed;
    }

    public async void OnButtonStartServer()
    {
        await SceneManager.LoadSceneAsync(_menuScene.name);
        //await SceneManager.LoadSceneAsync(_gameScene.name, LoadSceneMode.Additive);
        MPNetworkManager.Instance.StartServer();
    }

    private void OnDisable()
    {
        _authorization.Successed -= OnSuccessed;
    }

    private void OnSuccessed(int id)
    {
        if (id < 0)
        {
            StartOfflineMode();
            return;
        }

        _id = id;
        TryConnectWebSocketAsync();
        //SceneManager.LoadScene(_menuScene.name);
    }

    private async Task TryConnectWebSocketAsync()
    {
        await _webSocketClient.Connect();

        var authorizationData = new
        {
            type = "authorization",
            playerId = _id,
        };
        string json = JsonConvert.SerializeObject(authorizationData);

        _webSocketClient.SendMessageToServer(json);

        SceneManager.LoadScene(_menuScene.name);
    }

    private void StartOfflineMode()
    {
        SceneManager.LoadScene(_menuScene.name);
    }
}
