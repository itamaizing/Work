using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FireBallSkill : Skill
{
    [SerializeField] private Projectile _projectile;
    [SerializeField] private float _stunDuration = 4.5f;
    [SerializeField] private float _burnDuration = 5f;
    [SerializeField] private int _burnStacks = 3;

    private float _clickRadius = 0.5f;

    protected override bool IsCanCast => CheckCanCast();
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    private bool CheckCanCast()
    {
        return Targeting.GetTarget() != null && Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: false);

                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (!IsEnemyTarget(character))
                        Targeting.ClearTempTarget();
                    else
                    {
                        if (character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }

        targetInfo.AddTarget(Targeting.GetTempTarget()?.Character);
        Targeting.ClearTempTarget();
        callbackDataSaved(targetInfo);

        CastStarted += OnCastStarted;
    }

    private void OnCastStarted()
    {
        Hero.Move.LookAtTransform(Targeting.GetTarget().Character.transform);
        CastStarted -= OnCastStarted;
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget() != null)
            CmdCreateProjectile(Targeting.GetTarget().Character.gameObject);

        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Hero.Move.StopLookAt();
    }

    [Command]
    private void CmdCreateProjectile(GameObject target)
    {
        GameObject item = Instantiate(_projectile.gameObject, transform.position + Vector3.up, Quaternion.identity);

        var projectile = item.GetComponent<Projectile>();
        projectile.EndPointReached += OnEndPointReached;
        projectile.StartFly(target.transform, true);

        NetworkServer.Spawn(item);
    }

    private void OnEndPointReached(Projectile projectile, GameObject target)
    {
        projectile.EndPointReached -= OnEndPointReached;
        TargetRpcOnEndPointReached(target);
    }

    [TargetRpc]
    private void TargetRpcOnEndPointReached(GameObject target)
    {
        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damageValue),
            Type = Info.DamageType,
            PhysicAttackType = Info.AttackRangeType,
        };

        CmdApplyDamage(damage, target);
        CmdApplyEffects(target);
    }

    [Command]
    private void CmdApplyEffects(GameObject enemy)
    {
        var character = enemy.GetComponent<Character>();
        if (character == null) return;

        character.CharacterState.AddState(States.Stun, _stunDuration, 0, Hero.gameObject, name);

        for (int i = 0; i < _burnStacks; i++)
            character.CharacterState.AddState(States.BurningStacked, _burnDuration, 0, Hero.gameObject, name);
    }
}
