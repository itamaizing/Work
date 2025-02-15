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

    [Header("Effect LightShield")]
    [SerializeField] private GameObject lightShield;

    [Header("Effect DarkShield")]
    [SerializeField] private GameObject darkShield;
    #endregion

    [Header("Material Character")]
    [SerializeField] private Material materialCharacter;

    [Header("Weapon Character")]
    [SerializeField] private GameObject weapon;

    public GameObject FrozenStateEffect { get => frozenStateEffect; set => frozenStateEffect = value; }
    public GameObject Ice { get => ice; set => ice = value; }
    public GameObject Weapon { get => weapon; set => weapon = value; }
    public GameObject LightShield { get => lightShield; set => lightShield = value; }
    public GameObject DarkShield { get => darkShield; set => darkShield = value; }
    public Material MaterialCharacter { get => materialCharacter; set => materialCharacter = value; }
    public Material MaterialGhost { get => materialGhost; set => materialGhost = value; }
    public AudioClip FrostingAudio { get => frostingAudio; set => frostingAudio = value; }
    public AudioClip FrozenAudio { get => frozenAudio; set => frozenAudio = value; }


    private void Awake()
    {
        if (materialCharacter != null) materialCharacter.color = Color.white;
    }
}
