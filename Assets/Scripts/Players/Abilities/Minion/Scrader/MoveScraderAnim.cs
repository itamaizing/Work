using UnityEngine;

public class MoveScraderAnim : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private MoveScrader moveScrader;
    [SerializeField] private SpellMoveScraderTo spell;
    [SerializeField] private ScratchClaws scratchClaws;

    private void OnEnable()
    {
        spell.DoMove += HandleDoMove;
        scratchClaws.DoMove += HandleDoMove;
    }

    private void OnDisable()
    {
        spell.DoMove -= HandleDoMove;
        scratchClaws.DoMove -= HandleDoMove;
    }

    private void HandleDoMove(GameObject gameObject) => animator?.SetTrigger("MoveScared");
}