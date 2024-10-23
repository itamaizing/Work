using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestSpawn : Skill
{
    private Vector2 _position;

    protected override bool IsCanCast => true;

    protected override int AnimTriggerCastDelay => throw new System.NotImplementedException();

    protected override int AnimTriggerCast => throw new System.NotImplementedException();

    protected override IEnumerator CastJob()
    {
        if(Hero is HeroComponent hero)
        {
            hero.SpawnComponent.CmdSpawnUnit(0, _position);
        }
        yield return null;
    }

    protected override void ClearData()
    {
        _position = Vector2.zero;
    }

    protected override IEnumerator PrepareJob()
    {
        while(_position == Vector2.zero)
        {
            if (GetMouseButton)
            {
                _position = GetMousePoint();
            }
            yield return null;
        }
    }
}
