using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kcp2k;
using Mirror;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SignInLoginBootstrap : MonoBehaviour
{
    [SerializeField] private Authorization _authorization;
#if UNITY_EDITOR
    [SerializeField] private SceneAsset _menuScene;
    [SerializeField] private SceneAsset _gameScene;
#endif
    [SerializeField] private WebSocketClient _webSocketClient;

    private int _menuSceneIndex = 1;
    private int _gameSceneIndex = 2;
    private int _id;
    private string _bindIP = "localhost";
    private ushort _bindPort = 7777;
    private bool _isDedicatedServer = false;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_menuScene == null || _gameScene == null)
        {
            Debug.LogError("Scene assets are not assigned in the inspector.");
        }
        else if(GetSceneNameByBuildIndex(_menuSceneIndex) != _menuScene.name || GetSceneNameByBuildIndex(_gameSceneIndex) != _gameScene.name)
        { 
            Debug.LogError("Scene assets do not match the scenes in the build settings.");
            Debug.LogError(GetSceneNameByBuildIndex(_menuSceneIndex));
            Debug.LogError(_menuScene.name);
            Debug.LogError(GetSceneNameByBuildIndex(_gameSceneIndex));
            Debug.LogError(_gameScene.name);
        }
    }
#endif

    private void Start()
    {
        ParseCommandLineArguments();

        if (_isDedicatedServer)
            StartDedicatedServer();
    }

    private void OnEnable()
    {
        _authorization.Successed += OnSuccessed;
    }

    private void OnDisable()
    {
        _authorization.Successed -= OnSuccessed;
    }

    public void OnButtonStartServer()
    {
        StartDedicatedServer();
    }

    private void OnSuccessed(int id)
    {
        if (id < 0)
        {
            StartOfflineMode();
            return;
        }

        _id = id;
        TryConnectWebSocket();
        //SceneManager.LoadScene(_menuScene.name);
    }

    private void TryConnectWebSocket()
    {
        _webSocketClient.Connected += TryAuthorization;
        _webSocketClient.Connect();
    }

    private void TryAuthorization()
    {
        _webSocketClient.Connected -= TryAuthorization;
        var authorizationData = new
        {
            type = "authorization",
            playerId = _id,
        };
        string json = JsonConvert.SerializeObject(authorizationData);

        _webSocketClient.SendMessageToServer(json);

        SceneManager.LoadScene(_menuSceneIndex);
    }

    private void StartOfflineMode()
    {
        SceneManager.LoadScene(_menuSceneIndex);
    }

#if UNITY_EDITOR
    private string GetSceneNameByBuildIndex(int buildIndex)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        if (buildIndex >= 0 && buildIndex < scenes.Length)
        {
            string path = scenes[buildIndex].path;
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            return sceneName;
        }
        return null;
    }
#endif

    private void ParseCommandLineArguments()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("-port="))
            {
                string portStr = args[i].Substring("-port=".Length);
                if (ushort.TryParse(portStr, out ushort port))
                {
                    _bindPort = port;
                }
                _isDedicatedServer = true;
            }
            else if (args[i].StartsWith("-ip="))
            {
                _bindIP = args[i].Substring("-ip=".Length);
                _isDedicatedServer = true;
            }
        }

        Debug.LogError(_bindPort + _bindIP);
        Debug.Log(_bindPort + _bindIP);
    }

    private async Task StartDedicatedServer()
    {
        var transport = MPNetworkManager.Instance.GetComponent<KcpTransport>();

        if (transport == null)
        {
            Debug.LogError("Transport not found on NetworkManager");
            return;
        }
        transport.port = _bindPort;
        MPNetworkManager.Instance.networkAddress = _bindIP;

        await SceneManager.LoadSceneAsync(_menuSceneIndex);
        MPNetworkManager.Instance.StartServer();
    }
}
