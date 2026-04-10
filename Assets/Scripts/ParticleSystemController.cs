using UnityEngine;

public class ParticleSystemController : MonoBehaviour
{
    [SerializeField] ParticleSystem _Vfx;

    public void Play()
    {
        _Vfx.Play();
    }

    public void Stop()
    {
        _Vfx.Stop();
    }
}

