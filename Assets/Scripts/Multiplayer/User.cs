using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class User : NetworkBehaviour
{
    public static User Instance;

    private const string ID = "id";
    private const string BOTTLE = "bottle";

    private int _id = -37;
    private int _bottle;

    public void SetID(int id)
    {
        if(_id < 0)
        {
            _id = id;

            Dictionary<string, string> data = new Dictionary<string, string>()
            {
            {ID, _id.ToString()},
            };

            NetworkHTTP.Instance.Post(URLLibrary.GetBottle, data, Success);
        }
    }

    public override void OnStartClient()
    {
        if (isLocalPlayer && Instance == null && isOwned)
        {
            Instance = this;
        }
    }

    private void Success(string data)
    {
        if (int.TryParse(data, out int bottle))
        {
            Debug.Log(bottle);
            _bottle = bottle;
        }
        else
        {
            //Error?.Invoke(data);
        }
    }
}
