using UnityEngine;

public class MoveCreature : MoveComponent
{
   [SerializeField] protected float _moveDurationPerUnit = 0.2f;

    public float  MoveDurationPerUnit { get => _moveDurationPerUnit; set => _moveDurationPerUnit = value; }
}
