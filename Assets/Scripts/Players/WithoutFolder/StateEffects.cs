using UnityEngine;

public class StateEffects : MonoBehaviour
{
    #region Effects
    [Header("Effect Frozen")]
    [SerializeField] private GameObject frozenStateEffect;
    [SerializeField] private AudioClip frozenAudio;

    [Header("Effect Frosting")]
    [SerializeField] private GameObject ice;
    [SerializeField] private AudioClip frostingAudio;

    [Header("Effect Astral")]
    [SerializeField] private Material materialGhost;
    #endregion

    [Header("Material Character")]
    [SerializeField] private Material materialCharacter;

    [Header("Weapon Character")]
    [SerializeField] private GameObject weapon;

    public GameObject FrozenStateEffect { get => frozenStateEffect; set => frozenStateEffect = value; }
    public GameObject Ice { get => ice; set => ice = value; }
    public GameObject Weapon { get => weapon; set => weapon = value; }
    public Material MaterialCharacter { get => materialCharacter; set => materialCharacter = value; }
    public Material MaterialGhost { get => materialGhost; set => materialGhost = value; }
    public AudioClip FrostingAudio { get => frostingAudio; set => frostingAudio = value; }
    public AudioClip FrozenAudio { get => frozenAudio; set => frozenAudio = value; }

    private void Awake()
    {
        materialCharacter.color = Color.white;
    }
}
