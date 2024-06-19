using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CreateMMOCharacterMessage : NetworkMessage
{
    public int Index;
}

public class MultiplayerManager : NetworkManager
{
    [SerializeField, Scene] private string _onlineScene;
    [SerializeField] private List<HeroComponent> _heroList;

    private int _currentHeroIndex;

    public List<HeroComponent> HeroList { get => _heroList; set => _heroList = value; }

    public override void OnStartServer()
    {
        base.OnStartServer();

        NetworkServer.RegisterHandler<CreateMMOCharacterMessage>(OnCreateCharacter);
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        CreateMMOCharacterMessage characterMessage = new CreateMMOCharacterMessage
        {
            Index = _currentHeroIndex,
        };
        NetworkClient.Send(characterMessage);
    }

    private void OnCreateCharacter(NetworkConnectionToClient conn, CreateMMOCharacterMessage message)
    {
        GameObject gameobject = Instantiate(_heroList[message.Index]).gameObject;

        NetworkServer.AddPlayerForConnection(conn, gameobject);
        //ReplacePlayer(conn, message);
    }

    //public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    //{
    //    if (Utils.IsSceneActive(_onlineScene))
    //    {
    //        var player = Instantiate(_userPrefab);
    //        NetworkServer.AddPlayerForConnection(conn, player.gameObject);
    //    }
    //}

    //public override void OnStartClient()
    //{
    //    GameObject[] prefabs = Resources.LoadAll<GameObject>("MPPrefabs");
    //
    //    for (int i = 0; i < prefabs.Length; i++)
    //    {
    //        NetworkClient.RegisterPrefab(prefabs[i]);
    //    }
    //}

    public void SetPlayer(int heroIndex)
    {
        _currentHeroIndex = heroIndex;
    }

    public void ReplacePlayer(NetworkConnectionToClient conn, CreateMMOCharacterMessage newPrefab)
    {
        // Кэшировать ссылку на текущий объект игрока
        GameObject oldPlayer = conn.identity.gameObject;

        // Instantiate новый объект игрока и рассказать об этом клиентам
        // Включить значение true для параметра keepAuthority чтобы предотвратить смену владельца
        //NetworkServer.ReplacePlayerForConnection(conn, Instantiate(newPrefab.UserPrefab), true);

        // Удалите предыдущий объект игрока, который теперь был заменен
        // Для завершения замены требуется задержка.
        Destroy(oldPlayer, 0.5f);
    }
}
