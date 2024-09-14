using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class URLLibrary
{
    private const string _mainServer = "http://147.45.141.9/";
    private const string _localHost = "localhost";
    private const string _authorization = "ProjectGame/authorization.php";
    private const string _registration = "ProjectGame/registration.php";

    public static string Authorization { get { return _mainServer + _authorization; } }
    public static string Registration { get { return _mainServer + _registration; } }

    public static string MainServer => _mainServer;
    public static string LocalHost => _localHost;
}
