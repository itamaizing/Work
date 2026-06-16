using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class URLLibrary
{
    //private const string _mainServer = "http://147.45.246.50/game/";
    private const string _mainServer = "89.169.1.90";
    private const string _localHost = "localhost";
    private const string _authorization = "authorization.php";
    private const string _registration = "registration.php";
    private const string _setBottle = "SetBottle.php";
    private const string _getBottle = "GetBottle.php";
    private const string _setHeroData = "SetHeroData.php";
    private const string _getHeroData = "GetHeroData.php";
    private const string _startGame = "startGame.php";
    private const string _webSocketPort = "8888";
    private const string _webSocket = "ws://";
    private const string _addFriend = "AddFriend.php";
    private const string _getFriend = "GetFriendList.php";
    private const string _getUserLogin = "GerUserLogin.php";
    private const string _getFriendList = "GetFriendList.php";
    private const string _getFriendRequest = "GetFriendRequest.php";
    private const string _requestFriendship = "AddRequestFriendship.php";
    private const string _removeFriendshipRequest = "RemoveFriendRequst.php";
    private const string _removeFriend = "RemoveFriend.php";

    private readonly List<string> _heroName = new()
    {
        "testhero",
        "icedeath",
        "priest",
        "creeperpoison",
        "terrifyingelf",
        "kerrigan"
    };

    public static string Authorization { get { return GameFolder + _authorization; } }
    public static string Registration { get { return GameFolder + _registration; } }
    public static string SetBottle { get { return GameFolder + _setBottle; } }
    public static string GetBottle { get { return GameFolder + _getBottle; } }
    public static string SetHeroData { get { return GameFolder + _setHeroData; } }
    public static string GetHeroData { get { return GameFolder + _getHeroData; } }
    public static string StartGame { get { return GameFolder + _startGame; } }
    public static string WebSocket { get { return _webSocket + _mainServer + ":" + _webSocketPort; } }
    public static string HTTP { get { return "http://" + _mainServer + "/"; } }
    public static string GameFolder { get { return "http://" + _mainServer + "/" + "Work/"; } }
    public static string AddFriend { get { return GameFolder + _addFriend; } }
    public static string GetFriend { get { return GameFolder + _getFriend; } }
    public static string GetFriendList { get { return GameFolder + _getFriendList; } }
    public static string GetUserLogin { get { return GameFolder + _getUserLogin; } }
    public static string GetFriendRequest { get { return GameFolder + _getFriendRequest; } }
    public static string RequestFriendship { get { return GameFolder + _requestFriendship; } }
    public static string RemoveFriendshipRequest { get { return GameFolder + _removeFriendshipRequest; } }
    public static string RemoveFriend { get { return GameFolder + _removeFriend; } }

    public static string MainServer => _mainServer;
    public static string LocalHost => _localHost;
}
