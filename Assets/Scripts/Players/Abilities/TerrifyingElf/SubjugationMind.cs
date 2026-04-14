using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class SubjugationMind : Skill
{
    //private Character _target;
    private Vector3 _targetPoint = Vector3.positiveInfinity;

    protected override bool IsCanCast => Targeting.GetTarget() != null && Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("PullingHealthCastDelay");
    protected override int AnimTriggerCast => 0;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((ITargetable)(targetInfo.GetTargets()[0] as Character));
    }

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;

        if (target == null) yield break;

        CmdIntercept(target);

        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

        if (multiMagic != null)
        {
            foreach (var character in multiMagic.PopPendingTargets())
            {
                TryPayCost();
                CmdIntercept(character);
            }
        }

        AfterCastJob();
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        //_target = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Targetable == null && !_disactive)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), 0.5f);

                var temp = Targeting.GetTempTarget()?.Targetable as Character;

                if (temp != null)
                {
                    Targeting.SetTarget(temp);

                    if (multiMagic != null)
                        multiMagic.LastTarget = temp;

                    break;
                }
            }

            yield return null;
        }

        var target = Targeting.GetTarget()?.Character;

        if (target != null)
        {
            targetInfo.AddTarget(target);
            callbackDataSaved(targetInfo);
        }
    }

    [Command]
    private void CmdIntercept(Character character)
    {
        if (character == null) return;

        if (character is MinionComponent minion)
        {
            minion.SetAuthority(connectionToClient);

            if (Hero is HeroComponent hero)
            {
                hero.SpawnComponent.AddUnit(minion);
            }
        }
        else if (character is HeroComponent heroTarget)
        {
            var networkIdentity = heroTarget.GetComponent<NetworkIdentity>();
            if (networkIdentity == null) return;

            networkIdentity.RemoveClientAuthority();
            networkIdentity.AssignClientAuthority(connectionToClient);

            if (Hero is HeroComponent currentHero)
            {
                currentHero.SpawnComponent.AddUnit(heroTarget);
            }

            StartCoroutine(ReturnHeroControlAfterDelay(heroTarget, networkIdentity));
        }
    }

    private IEnumerator ReturnHeroControlAfterDelay(HeroComponent heroTarget, NetworkIdentity networkIdentity)
    {
        yield return new WaitForSeconds(4);

        if (networkIdentity != null)
        {
            networkIdentity.RemoveClientAuthority();
            networkIdentity.AssignClientAuthority(heroTarget.connectionToClient);

            if (Hero is HeroComponent currentHero)
            {
                currentHero.SpawnComponent.CmdRemoveUnit(heroTarget);
            }
        }
    }
}
