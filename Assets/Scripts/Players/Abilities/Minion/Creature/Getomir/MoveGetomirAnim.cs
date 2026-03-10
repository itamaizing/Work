using UnityEngine;

public class MoveGetomirAnim : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private MoveCreature _moveCreature;
    [SerializeField] private SpellMoveGetomirTo _spell;
    [SerializeField] private PowerStrike _powerStrike;

    private void OnEnable()
    {
        _spell.DoMove += HandleDoMove;
        _powerStrike.DoMove += HandleDoMove;
    }

    private void OnDisable()
    {
        _spell.DoMove -= HandleDoMove;
        _powerStrike.DoMove -= HandleDoMove;
    }

    private void HandleDoMove(GameObject gameObject) => _animator?.SetTrigger("MoveScared");
}
