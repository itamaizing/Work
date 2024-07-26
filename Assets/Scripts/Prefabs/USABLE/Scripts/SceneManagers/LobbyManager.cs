using Mirror;

public class LobbyManager : NetworkBehaviour
{
    private static LobbyManager instance;
    public static LobbyManager Instance => instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }
        instance = this;
    }

    public void OnPlayerConnect()
    {
        ScanGraph();
    }
    private void ScanGraph()
    {
        AstarPath activeAstarPath = AstarPath.active;

        if (activeAstarPath != null)
        {
            activeAstarPath.Scan();
        }
    }
}
