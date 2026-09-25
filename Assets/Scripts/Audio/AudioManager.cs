using UnityEngine;

public static class AudioManager
{
    public static LocalAudioSystem Local { get; private set; }
    public static NetworkAudioSystem Network { get; private set; }
    public static SoundDatabase Database { get; private set; }

    public static void RegisterLocal(LocalAudioSystem system) => Local = system;
    public static void RegisterNetwork(NetworkAudioSystem system) => Network = system;
}