using Mirror;
using UnityEngine;

public class FootstepSound : NetworkBehaviour
{
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioClip[] footstepClips;

    private void Start()
    {
        if (!isOwned) enabled = false;
    }

    [Client]
    public void PlayFootstep()
    {
        if (!isOwned) return;
        if (footstepClips.Length == 0 || footstepAudioSource == null) return;

        int index = Random.Range(0, footstepClips.Length);
        footstepAudioSource.PlayOneShot(footstepClips[index]);
    }
}
