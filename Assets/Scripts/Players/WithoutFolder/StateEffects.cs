using UnityEngine;

public class StateEffects : MonoBehaviour
{
    [Header("Effect Frost")]
    [SerializeField] private GameObject frozenStateEffect;
    [SerializeField] private GameObject ice;

    [SerializeField] private Material materialCharacter;

    public GameObject FrozenStateEffect { get => frozenStateEffect; set => frozenStateEffect = value; }
    public GameObject Ice { get => ice; set => ice = value; }
    public Material MaterialCharacter { get => materialCharacter; set => materialCharacter = value; }

    private void Awake()
    {
        materialCharacter.color = Color.white;
    }
}
