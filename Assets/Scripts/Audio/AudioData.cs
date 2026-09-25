using Mirror;
using UnityEngine;

public enum SfxPlayMode { Once, Loop }

public struct AudioData : NetworkMessage
{
    public int ClipHash;
    public float Volume;
    public float PitchDelta;
    public float VolumeDelta;
    public SfxPlayMode PlayMode;

    public bool HasPosition;
    public Vector3 Position;
    
    public uint FollowTargetNetId;

    public int RequestId;
}