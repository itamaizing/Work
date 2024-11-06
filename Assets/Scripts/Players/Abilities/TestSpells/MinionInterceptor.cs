using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionInterceptor : Skill
{
    private MinionComponent _target;

    protected override bool IsCanCast => true;

    protected override int AnimTriggerCastDelay => throw new System.NotImplementedException();

    protected override int AnimTriggerCast => throw new System.NotImplementedException();

    protected override IEnumerator CastJob()
    {
        CmdIntercept(_target.gameObject);
        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
    }

    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                var temp = GetRaycastTarget();

                if(temp is MinionComponent minion)
                    _target = minion;
            }
            yield return null;
        }
    }

    [Command]
    private void CmdIntercept(GameObject minion)
    {
        minion.GetComponent<MinionComponent>().SetAuthority(connectionToClient);
        
        if(Hero is HeroComponent hero)
        {
            hero.SpawnComponent.AddUnit(minion.GetComponent<MinionComponent>());
        }
    }
}
