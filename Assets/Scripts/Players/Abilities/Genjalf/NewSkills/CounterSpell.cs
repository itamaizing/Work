using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class CounterSpell : Skill
{
    [SerializeField] private ParticleSystem _particlePref;
    //[SerializeField, Range(0, 100)] private int _debuffChance = 15;

    protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private bool CheckCanCast()
    {
        return Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius && Targeting.GetTarget()?.Character != null;
    }

    public void AnimCastLight()
    {
        AnimStartCastCoroutine();
    }

    public void AnimLightEnd()
    {
        AnimCastEnded();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if(targetInfo.GetTargets().Count > 0 && targetInfo.GetTargets()[0] != null)
            Targeting.SetTarget(targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character != null)
        {
            CmdState(Targeting.GetTarget()?.Character.gameObject, 5);
            Targeting.GetTarget()?.Character.Abilities.CancleAllSkills();

            //Targeting.ClearTarget();
        }
        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        //_target = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Targetable == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget();
            }
            yield return null;
        }
        Targeting.SetTarget(Targeting.GetTempTarget()?.Targetable);
        Targeting.ClearTempTarget();

        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    private void CreateParticle(Vector3 position)
    {
        if (_particlePref != null)
        {
            GameObject item = Instantiate(_particlePref.gameObject, position, Quaternion.identity);
        }
    }

    [Command]
    protected void CmdCreateParticle(Vector3 position)
    {
        RpcCreateParticle(position);
    }

    [ClientRpc]
    private void RpcCreateParticle(Vector3 position)
    {
        CreateParticle(position);
    }

    [Command]
    private void CmdState(GameObject enemy, float time)
    {
        Character enemyChar = enemy.GetComponent<Character>();
        enemyChar.CharacterState.AddState(States.SchoolDebuff, time, 0, Hero.gameObject, name);
    }
}
