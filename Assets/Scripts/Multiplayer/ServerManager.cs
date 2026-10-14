using kcp2k;
using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private const string ACCEPTINVITE = "acceptInvite";
    private const string PLAYERID = "playerId";

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
        if (_instance != this) return;

        WebSocketClient.Instance.MessageReceived += OnMessageReceived;
        WebSocketClient.Instance.Disconnected += OnSocketDisconnected;
    }

    private void OnDestroy()
    {
        if (_instance != this) return;

        if (WebSocketClient.Instance != null)
        {
            WebSocketClient.Instance.MessageReceived -= OnMessageReceived;
            WebSocketClient.Instance.Disconnected -= OnSocketDisconnected;
        }
    }

    private void OnSocketDisconnected()
    {
        _groupManager?.Clear();
    }

    public void StartClient()
    {
        if (MPNetworkManager.Instance.UserID < 0)
        {
            OnServerRoomCreatedSuccess("localhost:7777");
            return;
        }

        if (_groupManager.HostId != null)
        {
            Debug.Log("Я гость группы, жду roomReady от хоста");
            return;
        }

        var startData = new
        {
            type = STARTGAME,
            playerId = MPNetworkManager.Instance.UserID,
            playerIdForAddGroup = _groupManager.GetPlayerInGroup(),
            gamemode = CurrentGameMode
        };
        string json = JsonConvert.SerializeObject(startData);

        Debug.Log($"startGame, группа: '{_groupManager.GetPlayerInGroup()}'");
        WebSocketClient.Instance.SendMessageToServer(json);
    }

    public void EnableMenu()
    {
        _menuEnv.gameObject.SetActive(true);
    }

    private void OnMessageReceived(Dictionary<string, string> dictionary, string json)
    {
        if (dictionary == null || dictionary.Count == 0)
            return;

        if (!dictionary.TryGetValue("type", out string type))
            return;

        switch (type)
        {
            case ROOMREADY:
                if (dictionary.TryGetValue(IPPORT, out string ipPort))
                    OnServerRoomCreatedSuccess(ipPort);
                break;

            case ADDEDTOGROUP:
                if (dictionary.TryGetValue(USERID, out string addedUserId))
                    _groupManager.AddPlayerInGroup(addedUserId);
                break;

            case LEFTGROUP:
                if (dictionary.TryGetValue(USERID, out string removedUserId))
                    _groupManager.RemovePlayerInGroup(removedUserId);
                break;

            case ACCEPTINVITE:
                if (dictionary.TryGetValue(PLAYERID, out string joinedId))
                    _groupManager.AddPlayerInGroup(joinedId);
                break;
        }
    }

    private void OnServerRoomCreatedSuccess(string ipAndPort)
    {
        if (NetworkClient.active)
        {
            Debug.LogWarning("NetworkClient уже активен, roomReady проигнорирован");
            return;
        }

        string[] parts = ipAndPort.Split(':');

        if (parts.Length == 2)
        {
            string ip = parts[0];
            string port = parts[1];

            Debug.Log($"Try connect - {ip}:{port}");

            var transport = MPNetworkManager.Instance.GetComponent<KcpTransport>();

            if (transport == null)
            {
                Debug.LogError("Transport not found on NetworkManager");
                return;
            }

            if (!ushort.TryParse(port, out ushort portNumber))
            {
                Debug.LogError($"Bad port: {port}");
                return;
            }

            transport.port = portNumber;
            MPNetworkManager.Instance.networkAddress = ip;

            MPNetworkManager.Instance.StartClient();
        }
        else
        {
            Debug.LogError($"Something wrong with ipAndPort: '{ipAndPort}' (parts: {parts.Length})");
        }
    }

    private void OnServerRoomCreatedError(string obj)
    {
        Debug.LogError($"Room create error: {obj}");
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
    }

    public void SetPlayer(HeroComponent hero)
    {
        _currentHeroIndex = _heroList.FindIndex(h => h.Data.Name == hero.Data.Name);

        if (LevelCharacterManager.Instance != null)
            LevelCharacterManager.Instance.SetHero(hero);
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

    private readonly List<int> _ids = new List<int>();

    public event Action Changed;

    public IReadOnlyList<int> Members => _ids;
    
    public int? HostId { get; private set; }

    public void SetHost(int id) => HostId = id;
    public void ClearHost() => HostId = null;

    public void SendInvite(int id)
    {
        var invData = new
        {
            type = INVITE,
            hostplayerId = MPNetworkManager.Instance.UserID,
            playerId = id
        };
        WebSocketClient.Instance.SendMessageToServer(JsonConvert.SerializeObject(invData));
    }

    public void AcceptInvite(int hostId)
    {
        var invData = new
        {
            type = ACCEPTINVITE,
            hostId = hostId,
            playerId = MPNetworkManager.Instance.UserID
        };
        WebSocketClient.Instance.SendMessageToServer(JsonConvert.SerializeObject(invData));
        SetHost(hostId);
        Debug.Log("Accept invite from host " + hostId);
    }

    public void AddPlayerInGroup(int id)
    {
        if (_ids.Contains(id)) return;
        _ids.Add(id);
        Changed?.Invoke();
    }

    public void AddPlayerInGroup(string id)
    {
        if (int.TryParse(id, out int idInt))
            AddPlayerInGroup(idInt);
        else
            Debug.LogError("Шляпа, а не ID");
    }

    public void RemovePlayerInGroup(int id)
    {
        if (HostId == id)
            ClearHost();

        if (_ids.Remove(id))
            Changed?.Invoke();
    }

    public void RemovePlayerInGroup(string id)
    {
        if (int.TryParse(id, out int idInt))
            RemovePlayerInGroup(idInt);
        else
            Debug.LogError("Шляпа, а не ID");
    }
    
    public void Leave()
    {
        WebSocketClient.Instance.SendMessageToServer(JsonConvert.SerializeObject(new { type = "leaveGroup" }));
        Clear();
    }

    public void Clear()
    {
        ClearHost();
        _ids.Clear();
        Changed?.Invoke();
    }

    public string GetPlayerInGroup() => string.Join(":", _ids);
}