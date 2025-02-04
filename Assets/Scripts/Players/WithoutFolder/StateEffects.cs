using UnityEngine;

public class StateEffects : MonoBehaviour
{
    [Header("Effect Frozen")]
    [SerializeField] private GameObject frozenStateEffect;
    [SerializeField] private AudioClip frozenAudio;

    [Header("Effect Frosting")]
    [SerializeField] private GameObject ice;
    [SerializeField] private AudioClip frostingAudio;

    [Header("Material Character")]
    [SerializeField] private Material materialCharacter;

    public GameObject FrozenStateEffect { get => frozenStateEffect; set => frozenStateEffect = value; }
    public GameObject Ice { get => ice; set => ice = value; }
    public Material MaterialCharacter { get => materialCharacter; set => materialCharacter = value; }
    public AudioClip FrostingAudio { get => frostingAudio; set => frostingAudio = value; }
    public AudioClip FrozenAudio { get => frozenAudio; set => frozenAudio = value; }

    private void Awake()
    {
        materialCharacter.color = Color.white;
    }
}
