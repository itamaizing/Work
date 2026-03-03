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
        return GetTargetCharacter() != null && Vector3.Distance(GetTargetCharacter().transform.position, transform.position) <= Radius;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = GetMousePoint();
                FindTarget(_clickRadius, clickPoint, canTargetHimself: false);

                if (GetTempTargetCharacter() is Character character)
                {
                    if (!IsEnemyTarget(character))
                        ClearTempTarget();
                    else
                    {
                        if (character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }

        targetInfo.AddTarget(GetTempTargetCharacter());
        ClearTempTarget();
        callbackDataSaved(targetInfo);

        CastStarted += OnCastStarted;
    }

    private void OnCastStarted()
    {
        Hero.Move.LookAtTransform(GetTargetCharacter().transform);
        CastStarted -= OnCastStarted;
    }

    protected override IEnumerator CastJob()
    {
        if (GetTargetCharacter() != null)
            CmdCreateProjectile(GetTargetCharacter().gameObject);

        yield return null;
    }

    protected override void ClearData()
    {
        ClearTarget();
        Hero.Move.StopLookAt();
    }

    [Command]
    private void CmdCreateProjectile(GameObject target)
    {
        GameObject item = Instantiate(_projectile.gameObject, transform.position + Vector3.up, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

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
            Type = DamageType,
            PhysicAttackType = AttackRangeType,
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
