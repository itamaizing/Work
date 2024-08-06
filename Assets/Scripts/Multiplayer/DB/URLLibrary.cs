using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class URLLibrary
{
    private const string _main = "http://147.45.141.9/";
    private const string _authorization = "ProjectGame/authorization.php";
    private const string _registration = "ProjectGame/registration.php";

    public static string Authorization { get { return _main + _authorization; } }
    public static string Registration { get { return _main + _registration; } }
}
