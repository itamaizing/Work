using UnityEngine;

public class MoveSpisnaciderAnim : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private MoveCreature _moveCreature;
    [SerializeField] private SpellMoveSpisnaciderTo _spell;
    [SerializeField] private SpittingAcid _spittingAcid;

    private void OnEnable()
    {
        _spell.DoMove += HandleDoMove;
        _spittingAcid.DoMove += HandleDoMove;
    }

    private void OnDisable()
    {
        _spell.DoMove -= HandleDoMove;
        _spittingAcid.DoMove -= HandleDoMove;
    }

    private void HandleDoMove(GameObject gameObject) => _animator?.SetTrigger("MoveScared");
}
