using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMPTraffic : NetworkBehaviour
{
    [SyncVar]
    private float sadsss;
    [SyncVar]
    private float asdasd;
    [SyncVar]
    private float asdsda;
    [SyncVar]
    private float asdss;
    [SyncVar]
    private float asdass;
    [SyncVar]
    private float asdasdasd;
    [SyncVar]
    private float asdsd;
    [SyncVar]
    private float asd;
    [SyncVar]
    private float sadsss78;
    [SyncVar]
    private float asda678sd;
    [SyncVar]
    private float asd678sda;
    [SyncVar]
    private float as678dss;
    [SyncVar]
    private float asda678ss;
    [SyncVar]
    private float as678dasdasd;
    [SyncVar]
    private float a678sdsd;
    [SyncVar]
    private float as678d;
    [SyncVar]
    private float sadhjksss;
    [SyncVar]
    private float ashjkdasd;
    [SyncVar]
    private float asdhjksda;
    [SyncVar]
    private float asdhjkss;
    [SyncVar]
    private float ashjkdass;
    [SyncVar]
    private float asdhjkasdasd;
    [SyncVar]
    private float ashjkdsd;
    [SyncVar]
    private float ashjkd;
    [SyncVar]
    private float sadhjksss78;
    [SyncVar]
    private float asdhjka678sd;
    [SyncVar]
    private float asdhjk678sda;
    [SyncVar]
    private float as6hjk78dss;
    [SyncVar]
    private float ashjkda678ss;
    [SyncVar]
    private float as6hjk78dasdasd;
    [SyncVar]
    private float a6hjk78sdsd;
    [SyncVar]
    private float ahjks678d;


    void Update()
    {
        if (Input.GetKey(KeyCode.Alpha1))
        {
            SendGameObject(this.gameObject);
        }
        if (Input.GetKey(KeyCode.Alpha2))
        {
            SendTransform(this.transform);
        }
        if (Input.GetKey(KeyCode.Alpha3))
        {
            SendVector(this.transform.position);
        }
        if (Input.GetKey(KeyCode.Alpha4))
        {
            SendFloat(14141.14331f);
        }
        if (Input.GetKey(KeyCode.Alpha5))
        {
            var x = new List<GameObject>()
            {
                this.gameObject,
                this.gameObject,
                this.gameObject,
                this.gameObject,
            };
            SendGameObject(x);
        }
        if (Input.GetKey(KeyCode.Alpha6))
        {
            SendGameObject(this.gameObject, this.gameObject, this.gameObject, this.gameObject);
        }
    }

    [Command]
    private void SendGameObject(GameObject gObject)
    {
        var x = gObject.transform.position;
    }
    [Command]
    private void SendGameObject(List<GameObject> gameObjects)
    {
        var x = gameObjects[0].transform.position;
    }
    [Command]
    private void SendGameObject(GameObject gObject, GameObject gObje2ct, GameObject gO3bject, GameObject gObj4ect)
    {
        var x = gObject.transform.position;
    }

    [Command]
    private void SendTransform(Transform item)
    {
        var x = item.transform.position;
    }

    [Command]
    private void SendVector(Vector3 item)
    {
        var x = item.x;
    }

    [Command]
    private void SendFloat(float f)
    {
        var x = f;
    }
}
