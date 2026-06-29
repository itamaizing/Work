using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

[Serializable]
public class FriendData
{
    public int id;
    public string login;
}

[Serializable]
public class FriendsResponse
{
    public bool success;
    public FriendData[] friends;
    public int count;
    public string error;
}

[Serializable]
public class OnlineFriendData
{
    public string playerId;
    public string status;
}

[Serializable]
public class OnlineCheckResponse
{
    public string type;
    public OnlineFriendData[] onlineFriends;
    public OnlineFriendData[] offlineFriends;
}

public class FriendUI : MonoBehaviour
{
    private const string LOGIN = "login";
    private const string USERID = "id";
    private const string ONLINECHECK = "onlineCheck";
    private const string ACCEPTINVITE = "acceptInvite";

    [SerializeField] private PlayerCardUI _playerCardPref;
    [SerializeField] private Button _addFriendButton;
    [SerializeField] private TMP_InputField _inputFieldFriendName;

    [SerializeField] private RectTransform[] _rectTransformsForUpdate;
    [SerializeField] private InviteRequestUI _inviteRequestPanel;

    [SerializeField] private GameObject _panelGroup;
    [SerializeField] private GameObject _panelOnline;
    [SerializeField] private GameObject _panelOffline;
    [SerializeField] private GameObject _panelRequests;

    [SerializeField] private Sprite _addSprite;
    [SerializeField] private Sprite _exitSprite;

    private string _friendName;
    private string _myLogin = "Me";
    private FriendData[] _friends;
    private FriendData[] _friendsRequst;
    private List<PlayerCardUI> _playersCards = new();
    private List<PlayerCardUI> _playersCardsGroup = new();
    private List<PlayerCardUI> _playersCardsFriendRequst = new();

    public event Action<FriendData[]> FriendListUpdated;
    public event Action FriendWentOffline;
    public event Action FriendWentOnline;
    public event Action FriendAcceptedInvite;

    public void Start()
    {
        DontDestroyOnLoad(transform.root.gameObject);
        gameObject.SetActive(false);
        WebSocketClient.Instance.MessageReceived += OnMessageReceived;
        _inviteRequestPanel.Accepted += OnInviteAccepted;
        WebSocketClient.Instance.Connected += OnSocketConnected;
    }

    private void OnEnable()
    {
        _inputFieldFriendName.onEndEdit.AddListener(SetFriendName);
        _addFriendButton.onClick.AddListener(TryAddFriend);

        if (WebSocketClient.Instance != null && WebSocketClient.Instance.IsConnected)
        {
            Dictionary<string, string> data1 = new Dictionary<string, string>()
            {
            {USERID, MPNetworkManager.Instance.UserID.ToString() },
            };
            NetworkHTTP.Instance.Post(URLLibrary.GetFriendList, data1, UpdateFriendList);
        }
        UpdateFriendRequests();
    }

    private void OnDisable()
    {
        _inputFieldFriendName.onEndEdit.RemoveListener(SetFriendName);
        _addFriendButton.onClick.RemoveListener(TryAddFriend);
    }

    private void OnDestroy()
    {
        WebSocketClient.Instance.MessageReceived -= OnMessageReceived;
        _inviteRequestPanel.Accepted -= OnInviteAccepted;
        WebSocketClient.Instance.Connected -= OnSocketConnected;
    }

    private void OnSocketConnected()
    {
        Dictionary<string, string> data1 = new Dictionary<string, string>()
            {
            {USERID, MPNetworkManager.Instance.UserID.ToString() },
            };
        NetworkHTTP.Instance.Post(URLLibrary.GetFriendList, data1, UpdateFriendList);
    }

    public void UpdateFriendList(string data)
    {  
        FriendsResponse response = JsonUtility.FromJson<FriendsResponse>(data);

        if (response.success)
        {
            _friends = response.friends;
        }
        else
        {
            Debug.LogError("Œ¯Ë·Í‡: " + response.error);
        }
        UpdateFriendUI();
        FriendListUpdated?.Invoke(_friends);
    }

    public void UpdateFriendUI()
    {
        if (_friends.Length == 0)
            return;

        string ids = "";
        foreach (FriendData friend in _friends)
        {
            ids += friend.id;
            ids += ":";
        }
        ids = ids.Remove(ids.Length - 1);

        var data = new
        {
            type = ONLINECHECK,
            playerIds = ids,
        };
        string json = JsonConvert.SerializeObject(data);

        WebSocketClient.Instance.SendMessageToServer(json);

        UpdateFriendRequests();
    }

    private void UpdateFriendRequests()
    {
        Dictionary<string, string> data = new Dictionary<string, string>()
        {
            {USERID, MPNetworkManager.Instance.UserID.ToString() },
        };
        NetworkHTTP.Instance.Post(URLLibrary.GetFriendRequest, data, OnFriendRequest);
    }

    private void OnFriendRequest(string data)
    {
        FriendsResponse response = JsonUtility.FromJson<FriendsResponse>(data);

        if (response.success)
        {
            _friendsRequst = response.friends;
        }
        else
        {
            Debug.LogError("Œ¯Ë·Í‡: " + response.error);
        }

        foreach (FriendData friend in _friendsRequst)
        {
            if (_playersCardsFriendRequst.Any(card => card.Id == friend.id) == false)
            {
                var card = Instantiate(_playerCardPref, _panelRequests.transform);
                _playersCardsFriendRequst.Add(card);

                card.Init(friend.login, friend.id);
                card.SetButtn(0, _addSprite, AcceptFriendship);
                card.SetButtn(1, _exitSprite, CancelFriendships);
            }

        }
    }

    private void CancelFriendships(PlayerCardUI card)
    {
        _playersCardsFriendRequst.Remove(card);

        Dictionary<string, string> data = new Dictionary<string, string>()
        {
            {USERID, MPNetworkManager.Instance.UserID.ToString() },
            {"friendId", card.Id.ToString()}
        };
        NetworkHTTP.Instance.Post(URLLibrary.RemoveFriendshipRequest, data);

        Destroy(card.gameObject);
    }

    private void AcceptFriendship(PlayerCardUI card)
    {
        AddFriend(card.Name);

        CancelFriendships(card);
    }

    private void OnMessageReceived(Dictionary<string, string> dictionary, string json)
    {
        var baseMsg = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        string type = baseMsg["type"].ToString();

        switch (type)
        {
            case "onlineCheckResponse":
                OnOnlineCheckResponse(json);
                break;
            case "inviteRequest":
                OnInviteRequest(dictionary);
                break;   
            case "acceptInvite":
                OnInviteAccept(dictionary);
                FriendAcceptedInvite?.Invoke();
                break;
            case "needUpdateFriendUI":
                UpdateFriendUI();
                break;
            case "friend_offline":
                UpdateFriendUI();
                FriendWentOffline?.Invoke();
                break;
            case "friend_online":
                UpdateFriendUI();
                FriendWentOnline?.Invoke();
                break;
        }
    }

    private void SetFriendName(string friendName)
    {
        _friendName = friendName;
    }

    private void TryAddFriend()
    {
        Dictionary<string, string> data = new Dictionary<string, string>()
        {
            {USERID, MPNetworkManager.Instance.UserID.ToString() },
            {LOGIN, _friendName}
        };
        NetworkHTTP.Instance.Post(URLLibrary.RequestFriendship, data);
    }

    private void AddFriend(string name)
    {
        Dictionary<string, string> data = new Dictionary<string, string>()
        {
            {USERID, MPNetworkManager.Instance.UserID.ToString() },
            {LOGIN, name}
        };
        NetworkHTTP.Instance.Post(URLLibrary.AddFriend, data, OnFriendAdded);
    }

    private void OnFriendAdded(string data)
    {
        Dictionary<string, string> data1 = new Dictionary<string, string>()
        {
            {USERID, MPNetworkManager.Instance.UserID.ToString() },
        };
        NetworkHTTP.Instance.Post(URLLibrary.GetFriendList, data1, UpdateFriendList);
    }

    private void OnOnlineCheckResponse(string json)
    {
        foreach (var item in _playersCards)
        {
            Destroy(item.gameObject);
        }
        _playersCards.Clear();

        var response = JsonConvert.DeserializeObject<OnlineCheckResponse>(json);

        foreach (var onlineFriend in response.onlineFriends)
        {
            var card = Instantiate(_playerCardPref, _panelOnline.transform);
            _playersCards.Add(card);

            int index = Array.FindIndex(_friends, friend => friend.id == int.Parse(onlineFriend.playerId));

            if (index != -1)
            {
                card.Init(_friends[index].login, int.Parse(onlineFriend.playerId));
                card.SetButtn(0, _addSprite, SendGroupInvite);
            }
        }

        foreach (var offlineFriend in response.offlineFriends)
        {
            var card = Instantiate(_playerCardPref, _panelOffline.transform);
            _playersCards.Add(card);

            int index = Array.FindIndex(_friends, friend => friend.id == int.Parse(offlineFriend.playerId));

            if (index != -1)
            {
                card.Init(_friends[index].login, int.Parse(offlineFriend.playerId));
            }
        }
        UpdateRect();
    }

    private void OnInviteRequest(Dictionary<string, string> dictionary)
    {
        string id = dictionary["hostId"];
        int index = Array.FindIndex(_friends, friend => friend.id == int.Parse(id));
        _inviteRequestPanel.gameObject.SetActive(true);
        _inviteRequestPanel.SetInvater(_friends[index].login, _friends[index].id);
    }

    private void OnInviteAccepted(bool accept, int id)
    {
        if (accept == false)
            return;

        var data = new
        {
            type = ACCEPTINVITE,
            hostId = id,
            playerId = MPNetworkManager.Instance.UserID
        };
        string json = JsonConvert.SerializeObject(data);

        WebSocketClient.Instance.SendMessageToServer(json);

        ServerManager.Instance.GroupManager.AddPlayerInGroup(id.ToString());

        var card = Instantiate(_playerCardPref, _panelGroup.transform);
        _playersCardsGroup.Add(card);
        int index = Array.FindIndex(_friends, friend => friend.id == id);
        if (index != -1)
        {
            card.Init(_friends[index].login, id);
        }

        card = Instantiate(_playerCardPref, _panelGroup.transform);
        _playersCardsGroup.Add(card);
        id = MPNetworkManager.Instance.UserID;
        card.Init(_myLogin, id);
        card.SetButtn(0, _exitSprite, ExitGroup);
        UpdateRect();
    }

    private void OnInviteAccept(Dictionary<string, string> dictionary)
    {
        string id = dictionary["playerId"];
        int index = Array.FindIndex(_friends, friend => friend.id == int.Parse(id));

        ServerManager.Instance.GroupManager.AddPlayerInGroup(id.ToString());

        var card = Instantiate(_playerCardPref, _panelGroup.transform);
        _playersCardsGroup.Add(card);
        if (index != -1)
        {
            card.Init(_friends[index].login, int.Parse(id));
            card.SetButtn(0, _exitSprite, ExitGroup);
        }

        var idInt = MPNetworkManager.Instance.UserID;
        foreach (var item in _playersCardsGroup)
        {
            if (item.Id == idInt)
                return;
        }

        card = Instantiate(_playerCardPref, _panelGroup.transform);
        _playersCardsGroup.Add(card);
        card.Init(_myLogin, idInt);
        card.SetButtn(0, _exitSprite, ExitGroup);
        UpdateRect();
    }


    private void ExitGroup(PlayerCardUI uI)
    {
        if (uI.Id == MPNetworkManager.Instance.UserID)
        {
            foreach (var card in _playersCardsGroup)
            {
                Destroy(card.gameObject);
            }
            _playersCardsGroup.Clear();
            ServerManager.Instance.GroupManager.Clear();
            return;
        }

        ServerManager.Instance.GroupManager.RemovePlayerInGroup(uI.Id);
        int index = _playersCardsGroup.IndexOf(uI);
        Destroy(uI.gameObject);
        _playersCardsGroup.RemoveAt(index);

        if (_playersCardsGroup.Count <= 1)
        {
            foreach (var card in _playersCardsGroup)
            {
                Destroy(card.gameObject);
            }
            _playersCardsGroup.Clear();
        }
        UpdateRect();
    }

    private void SendGroupInvite(PlayerCardUI card)
    {
        ServerManager.Instance.GroupManager.SendInvite(card.Id);
    }

    private void UpdateRect()
    {
        foreach (var item in _rectTransformsForUpdate)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(item);
        }
    }

    private void SendUpdateRequest(int id)
    {
        var data = new
        {
            type = "needUpdateFriendUI",
            id = id
        };
        string json = JsonConvert.SerializeObject(data);

        WebSocketClient.Instance.SendMessageToServer(json);
    }
}
