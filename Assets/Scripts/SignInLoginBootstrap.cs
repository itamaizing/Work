using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
public class SignInLoginBootstrap : MonoBehaviour, ISerializationCallbackReceiver
{
    //SceneAsset - EditorOnly, с этим не получится сбилдить
    [SerializeField] private SceneAsset _menuScene;
    [SerializeField] private SceneAsset _gameScene;

    public void OnBeforeSerialize() => FillSceneNames();
    public void OnAfterDeserialize() { }

    private void FillSceneNames()
    {
        _menuSceneName = _menuScene.name;
        _gameSceneName = _gameScene.name;
    }
#else
public class SignInLoginBootstrap : MonoBehaviour
{
#endif
    [SerializeField] private string _menuSceneName, _gameSceneName;
    [SerializeField] private Authorization _authorization;
    [SerializeField] private WebSocketClient _webSocketClient;
    

    private int _id;

    private void OnEnable()
    {
        _authorization.Successed += OnSuccessed;
    }

    public async void OnButtonStartServer()
    {
        await SceneManager.LoadSceneAsync(_menuSceneName);
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

        SceneManager.LoadScene(_menuSceneName);
    }

    private void StartOfflineMode()
    {
        SceneManager.LoadScene(_menuSceneName);
    }

#if UNITY_SERVER
    private async void Start()
    {
        // Костыльно, но просто LoadScene не работает, если не менять online scene в NetworkHTTP на SignIn (awake не стреляли)
        // Но тогда клиент не переходит на сцену после подключения
        Debug.Log("AutomaticallyStartingHeadlessServer");
        Debug.Log(_menuSceneName);
        MPNetworkManager.Instance.StartServer();
        MPNetworkManager.Instance.ServerChangeScene(_menuSceneName);
        await SceneManager.LoadSceneAsync(_gameSceneName, LoadSceneMode.Additive);
        //await Task.Delay(1000);
        Debug.Log($"{ServerManager.Instance} - awake() servManager");
        MPNetworkManager.Instance.ServerChangeScene(_gameSceneName);
        //await Task.Delay(1000);
        Debug.Log($"{ServerManager.Instance} server manager is still here");
        Debug.Log("===== Server Started =====");
    }
#endif
}
