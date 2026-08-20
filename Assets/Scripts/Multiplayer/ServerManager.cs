using kcp2k;
using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;

public class ServerManager : NetworkBehaviour
{
    private const string STARTGAME = "startGame";
    private const string HEROINDEX = "heroIndex";
    private const string GAMEMODE = "gamemode";
    private const string USERID = "userID";
    private const string GROUPUSERS = "groupUsers";
    private const string ROOMREADY = "roomReady";
    private const string IPPORT = "ipAndPort";
    private const string ADDEDTOGROUP = "AddedToGroup";
    private const string LEFTGROUP = "leftGroup";

    [SerializeField] private List<NetworkRoomsManager> _managers;
    [SerializeField] private List<HeroComponent> _heroList;

    [SerializeField] private GameObject _menuEnv;

    private static ServerManager _instance;
    private int _currentHeroIndex = 0;
    private GameMode _currentGameMode = GameMode.GMSingle;
    private GroupManager _groupManager;

    public GameMode CurrentGameMode => _currentGameMode;
    public static ServerManager Instance => _instance;
    public List<HeroComponent> HeroList => _heroList;
    public int CurrentHeroIndex { get => _currentHeroIndex; }
    public GroupManager GroupManager { get => _groupManager; }

    public void Awake()
    {
        Debug.Log("ServerAwake");
        if (_instance != null)
        {
            _groupManager = null;
            Destroy(this);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
            _groupManager = new GroupManager();
        }
        _menuEnv.gameObject.SetActive(true);
    }

    private void Start()
    {
        WebSocketClient.Instance.MessageReceived += OnMessageReceived;
    }

    private void OnDestroy()
    {
        WebSocketClient.Instance.MessageReceived -= OnMessageReceived;
    }

    public void StartClient()
    {
        //_menuEnv.gameObject.SetActive(false);
        //AddPlayer(User.Instance.gameObject, _currentHeroIndex, _currentGameMode);

        if (MPNetworkManager.Instance.UserID < 0)
        {
            OnServerRoomCreatedSuccess("localhost:7777");
            return;
        }

        //request
        /*
        Dictionary<string, string> data = new Dictionary<string, string>()
        {
            {HEROINDEX, _currentHeroIndex.ToString()},
            {GAMEMODE, _currentGameMode.ToString()},
            {USERID, MPNetworkManager.Instance.UserID.ToString()},
            {GROUPUSERS, MPNetworkManager.Instance.UserID.ToString()}
        };
        NetworkHTTP.Instance.Post(URLLibrary.StartGame, data, OnServerRoomCreatedSuccess, OnServerRoomCreatedError);*/

        var startData = new
        {
            type = STARTGAME,
            playerId = MPNetworkManager.Instance.UserID,
            playerIdForAddGroup = _groupManager.GetPlayerInGroup(),
            gamemode = CurrentGameMode
        };
        string json = JsonConvert.SerializeObject(startData);

        WebSocketClient.Instance.SendMessageToServer(json);
    }

    public void EnableMenu()
    {
        _menuEnv.gameObject.SetActive(true);
    }

    private void OnMessageReceived(Dictionary<string, string> dictionary, string json)
    {
        if (dictionary.Count == 0)
            return;

        string type = dictionary["type"].ToString();

        switch (type)
        {
            case ROOMREADY:
                string ipPort = dictionary[IPPORT].ToString();
                OnServerRoomCreatedSuccess(ipPort);
                break;

            case ADDEDTOGROUP:
                string addedUserId = dictionary[USERID].ToString();
                _groupManager.AddPlayerInGroup(addedUserId);
                break;

            case LEFTGROUP:
                string removedUserId = dictionary[USERID].ToString();
                _groupManager.RemovePlayerInGroup(removedUserId);
                break;
        }
    }


    private void OnServerRoomCreatedSuccess(string ipAndPort)
    {
        string ip;  
        string port;

        string[] parts = ipAndPort.Split(':');

        if (parts.Length == 2)
        {
            ip = parts[0];
            port = parts[1];

            Debug.Log($"Try connect - {ip}:{port}");

            var transport = MPNetworkManager.Instance.GetComponent<KcpTransport>();

            if (transport == null)
            {
                Debug.LogError("Transport not found on NetworkManager");
                return;
            }
            transport.port = ushort.Parse(port);
            MPNetworkManager.Instance.networkAddress = ip;

            MPNetworkManager.Instance.StartClient();
        }
        else
        {
            Debug.LogError("Samthing wrong " + parts.Length);
            Debug.LogError(parts);
        }
    }

    private void OnServerRoomCreatedError(string obj)
    {
        throw new NotImplementedException();
    }

    [Command(requiresAuthority = false)]
    private void AddPlayer(GameObject user, int characterIndex, GameMode gameMode)
    {
        StartCoroutine(AddPlayerInRoomJob(user, characterIndex, gameMode));
    }

    private int GetManagerIndex(GameMode mode)
    {
        for (int i = 0; i < _managers.Count; i++)
        {
            if (_managers[i].GameMode == mode)
                return i;
        }
        Debug.LogError("manager not found");
        return -37;
    }

    private IEnumerator AddPlayerInRoomJob(GameObject user, int characterIndex, GameMode gameMode)
    {
        GameObject player = Instantiate(_heroList[characterIndex].gameObject);
        NetworkServer.Spawn(player, user);

        int index = GetManagerIndex(gameMode);

        yield return StartCoroutine(_managers[index].AddPlayerJob(player));

        user.GetComponent<User>().connectionToClient.Send(new SceneMessage { sceneName = _managers[index].Scene, sceneOperation = SceneOperation.LoadAdditive });
        //SceneManager.MoveGameObjectToScene(user, SceneManager.GetSceneAt(SceneManager.sceneCount - 1));
    }

    public void SetPlayer(HeroComponent hero)
    {
        _currentHeroIndex = _heroList.FindIndex(h => h.Data.Name == hero.Data.Name);

        if (LevelCharacterManager.Instance != null)
        {
            LevelCharacterManager.Instance.SetHero(hero);

            RequestHeroLevelFromServer(hero);
        }
    }

    private void RequestHeroLevelFromServer(HeroComponent hero)
    {
        if (MPNetworkManager.Instance == null || MPNetworkManager.Instance.UserID < 0)
            return;

        Dictionary<string, string> data = new Dictionary<string, string>()
        {
            { "id", MPNetworkManager.Instance.UserID.ToString() },
            { "heroName", hero.Data.Name }
        };
        
        Debug.LogError("heroName: "+ hero.Data.Name);

        NetworkHTTP.Instance.PostGetHeroData(
            data,
            success: json => OnHeroLevelReceived(hero, json),
            error: err => Debug.LogWarning($"[ServerManager] Сервер недоступен, остаёмся на PlayerPrefs: {err}")
        );
    }

    private void OnHeroLevelReceived(HeroComponent hero, string json)
    {
        if (string.IsNullOrEmpty(json) || json.Contains("\"success\":false"))
        {
            Debug.LogWarning($"[ServerManager] GetHeroData вернул ошибку: {json}");
            return;
        }

        // Игрок мог уже переключиться на другого героя, пока летел запрос — не применяем устаревший ответ
        if (!LevelCharacterManager.Instance.TryGetCurrentHero(out var currentHero) || currentHero != hero)
            return;

        HeroData data = NetworkHTTP.ConvertInHeroData(json);
        LevelCharacterManager.Instance.ApplyServerLevelData(hero, data.lvl, data.exp, data.skillpoints);
    }

    public void SetMode(GameMode mode)
    {
        _currentGameMode = mode;
    }
}

public class GroupManager
{
    private const string INVITE = "invite";
    private const string ACCEPTINVITE = "acceptInvite";
    private const string PLAYERID = "playerId";

    private List<int> _ids = new List<int>();
    
    public void SendInvite(int id)
    {
        var invData = new
        {
            type = INVITE,
            hostplayerId = MPNetworkManager.Instance.UserID,
            playerId = id
        };
        string json = JsonConvert.SerializeObject(invData);

        WebSocketClient.Instance.SendMessageToServer(json);
    }    

    public void AcceptInvite(int id)
    {
        var invData = new
        {
            type = ACCEPTINVITE,
            HostplayerId = MPNetworkManager.Instance.UserID,
            playerId = id
        };
        string json = JsonConvert.SerializeObject(invData);

        WebSocketClient.Instance.SendMessageToServer(json);
        Debug.Log("Try inv" + id);
    }

    public void AddPlayerInGroup(int id)
    {
        _ids.Add(id);
    }

    public void AddPlayerInGroup(string id)
    {
        if (int.TryParse(id, out int idInt))
            _ids.Add(idInt);
        else
            Debug.LogError("Шляпа, а не ID");
    }

    public void RemovePlayerInGroup(int id)
    {
        _ids.Remove(id);
    }

    public void Clear() { _ids.Clear(); }

    public void RemovePlayerInGroup(string id)
    {
        if (int.TryParse(id, out int idInt))
            _ids.Remove(idInt);
        else
            Debug.LogError("Шляпа, а не ID");
    }

    public string GetPlayerInGroup()
    {
        string playerIdForAddGroup = "";

        if(_ids.Count == 0)
            return playerIdForAddGroup;

        foreach (var id in _ids)
        {
            playerIdForAddGroup = playerIdForAddGroup + id + ":";
        }
        playerIdForAddGroup = playerIdForAddGroup.Remove(playerIdForAddGroup.Length - 1);

        return playerIdForAddGroup;
    }
}