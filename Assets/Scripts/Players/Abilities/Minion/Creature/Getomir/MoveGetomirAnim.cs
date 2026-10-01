using UnityEngine;

public class MoveGetomirAnim : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private PowerStrike _powerStrike;

    private void OnEnable()
    {
        _powerStrike.DoMove += HandleDoMove;
    }

    private void OnDisable()
    {
        _powerStrike.DoMove -= HandleDoMove;
    }

    private void HandleDoMove(GameObject gameObject) => _animator?.SetTrigger("MoveScared");
}
