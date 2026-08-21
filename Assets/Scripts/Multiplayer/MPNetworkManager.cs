using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
//using UnityEngine.SceneManagement;
//using static UnityEditor.Progress;

[Serializable]
public class GameRule
{
    public GameMode GameMode;
    public GameRules GameObjcet;
}

public class MPNetworkManager : NetworkManager
{
    public static MPNetworkManager Instance;

    [SerializeField] private List<HeroComponent> _heroList;
    [SerializeField] private List<GameRule> _gameRules;
    [SerializeField] private int _currentGameRulesIndex;

    private List<GameObject> _players = new List<GameObject>();
    [SerializeField] private int _userID = -37;
    private GameRules _currentGameRules;

    public int UserID { get => _userID; set { if(_userID == -37) _userID = value; } }
    public List<GameObject> Players => _players;

    public List<HeroComponent> HeroList { get => _heroList; set => _heroList = value; }
    public int CurrentGameRulesIndex { get => _currentGameRulesIndex; set => _currentGameRulesIndex = value; }
    public GameMode CurrentGameMode { get; set; }

    public event Action ConnectClosed;
    public event Action NewConnected;

    override public void Awake()
    {
        base.Awake();

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (_currentGameRules != null)
        {
            Destroy(_currentGameRules);
            _currentGameRules = null;
        }
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        NewConnected?.Invoke();

        if (_currentGameRules == null)
            CreateGameRules();

        Debug.Log("TEST");
    }

    public bool IsServer()
    {
        return _userID > 0;
    }
    
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        _players.Clear();
        ConnectClosed?.Invoke();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        _players.Clear();
        ConnectClosed?.Invoke();
    }

    public void SetServerAddress(string ip, string port)
    {
        networkAddress = ip;

        GetComponent<TelepathyTransport>().port = ushort.Parse(port);

        Debug.Log($"Server Address changed on: {ip}:{port}");
    }

    public void ConnectToServer()
    {
        if (isNetworkActive == false)
            StartClient();
        else
            Debug.LogError("Already connected");

        Debug.Log("TEST");
    }

    public void AddPlayer(GameObject player)
    {
        Players.Add(player);

        _currentGameRules.CurrentPlayers++;

        if (_currentGameRules.MaxPlayers == _currentGameRules.CurrentPlayers)
            StartGame();
    }

    private Dictionary<GameMode, GameRules> GetGameRulesAsDictionary()
    {
        var dict = new Dictionary<GameMode, GameRules>();
        foreach (var rule in _gameRules)
        {
            dict[rule.GameMode] = rule.GameObjcet;
        }
        return dict;
    }

    private void StartGame()
    {
        _currentGameRules.Init();
        _currentGameRules.IsStarted = true;
        _currentGameRules.GameStartServer(_currentGameRules.SpawnPoints);
    }

    private void CreateGameRules()
    {
        var dictionary = GetGameRulesAsDictionary();
        GameObject obj = Instantiate(dictionary[CurrentGameMode].gameObject);
        NetworkServer.Spawn(obj);

        GameRules item = obj.GetComponent<GameRules>();
        _currentGameRules = item;
    }
}
