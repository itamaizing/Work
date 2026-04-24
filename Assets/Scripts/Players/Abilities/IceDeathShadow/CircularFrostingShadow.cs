using Mirror;
using System.Collections;
using UnityEngine;

public class CircularFrostingShadow : NetworkBehaviour
{
    private Character _owner;
    private float _remainingDelay;
    private float _radius;

    private Coroutine _routine;

    public void Init(Character owner, float remainingDelay, float radius)
    {
        _owner = owner;
        _remainingDelay = remainingDelay;
        _radius = radius;
    }

    public void StartShadowLogic()
    {
        if (!isServer) return;
        if (_routine != null) return;

        _routine = StartCoroutine(ShadowRoutine());
    }

    private IEnumerator ShadowRoutine()
    {
        if (_remainingDelay > 0f) yield return new WaitForSeconds(_remainingDelay);
        if (_owner == null || _owner.IsDead) yield break;

        ExecuteFrostingLogic();
    }

    private void ExecuteFrostingLogic()
    {
        var energy = (Energy)_owner.Resources[ResourceType.Energy];
        if (energy == null) return;

        float baseDuration = 2f;
        float usedEnergy;
        float duration;

        if (energy.CurrentValue >= 30f)
        {
            usedEnergy = 30f;
            duration = baseDuration + 3f;
        }
        else
        {
            usedEnergy = energy.CurrentValue;
            duration = baseDuration + usedEnergy / 10f;
        }

        if (usedEnergy <= 0f) return;

        energy.CmdUse(usedEnergy);

        Collider[] hits = Physics.OverlapSphere(_owner.transform.position, _radius);

        foreach (var col in hits)
        {
            if (!col.TryGetComponent(out Character target)) continue;
            if (target == _owner) continue;

            target.CharacterState.AddState(States.Frosting, duration, 0, _owner.gameObject, "CircularFrostingShadow");
        }
    }

    public override void OnStopServer()
    {
        if (_routine != null)
            StopCoroutine(_routine);
    }
}