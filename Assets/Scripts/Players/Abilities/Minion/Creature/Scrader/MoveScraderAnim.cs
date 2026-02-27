using UnityEngine;

public class MoveScraderAnim : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private MoveCreature _moveCreature;
    [SerializeField] private SpellMoveScraderTo _spell;
    [SerializeField] private ScratchClaws _scratchClaws;

    private void OnEnable()
    {
        _spell.DoMove += HandleDoMove;
        _scratchClaws.DoMove += HandleDoMove;
    }

    private void OnDisable()
    {
        _spell.DoMove -= HandleDoMove;
        _scratchClaws.DoMove -= HandleDoMove;
    }

    private void HandleDoMove(GameObject gameObject) => _animator?.SetTrigger("MoveScared");
}