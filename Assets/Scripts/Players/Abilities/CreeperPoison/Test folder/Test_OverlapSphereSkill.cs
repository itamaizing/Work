using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test_OverlapSphereSkill : Skill
{
    [SerializeField] private Character _player;
    private Vector3 _mousePos = Vector3.positiveInfinity;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => true;


    protected override void ClearData()
    {
        _mousePos = Vector3.positiveInfinity;
    }

    protected override IEnumerator PrepareJob()
    {
        while (!Input.GetMouseButtonDown(2))
        {
            Collider[] targets = Physics.OverlapSphere(_player.transform.position, Radius, _targetsLayers);
            if (targets.Length > 0)
            {
                foreach (Collider target in targets)
                {
                    Debug.Log("TestSkill / CastJob / target.name = " + target.name);
                }
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {

            yield return null;
        
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_player.transform.position, Radius);
    }
}
