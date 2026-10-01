using UnityEngine;

public static class AudioManager
{
    public static LocalAudioSystem Local { get; private set; }
    public static NetworkAudioSystem Network { get; private set; }
    public static SoundDatabase Database { get; private set; }

    private static int _requestCounter;

    public static void RegisterLocal(LocalAudioSystem system)
    {
        Local = system;
        Database = system != null ? system.Database : null;
    }

    public static void RegisterNetwork(NetworkAudioSystem system) => Network = system;

    public static int NextRequestId() => ++_requestCounter;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Local = null;
        Network = null;
        Database = null;
        _requestCounter = 0;
    }
}