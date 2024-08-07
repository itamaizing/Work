using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Authorization : MonoBehaviour
{
    private const string LOGIN = "login";
    private const string PASSWORD = "password";

    private int _id;
    private string _login;
    private string _password;

    public event Action<string> Error;
    public event Action<int> Successed;

    public void SetLogin(string login)
    {
        _login = login;
    }

    public void SetPassword(string password)
    {
        _password = password;
    }

    public void SignIn()
    {
        if(string.IsNullOrEmpty(_login) || string.IsNullOrEmpty(_password))
        {
            Error?.Invoke("login or pass not validate");
            Debug.LogError("login or pass not validate");
            return;
        }
        Dictionary<string, string> data = new Dictionary<string, string>()
        {
            {LOGIN, _login},
            {PASSWORD, _password }
        };

        NetworkHTTP.Instance.Post(URLLibrary.Authorization, data, Success);
    }

    private void Success(string data)
    {
        if(int.TryParse(data, out int id))
        {
            _id = id;
            Successed?.Invoke(id);
        }
        else
        {
            Error?.Invoke(data);
        }
    }
}
