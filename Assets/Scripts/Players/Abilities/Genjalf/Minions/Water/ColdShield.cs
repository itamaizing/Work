using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ColdShield : MoveSkill
{
    [SerializeField] private Shield _shieldPref;
    [SerializeField] private float _baffDuration = 9;
    
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => CheckCanCast();

    private float _clickRadius = 0.5f;
    private GameObject _shield;

    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    private bool CheckCanCast()
    {
        var target = Targeting.GetTarget()?.Character;
        if (target == null) return false;
        return Vector3.Distance(target.transform.position, transform.position) <= AreaInfo.Radius;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((Character)targetInfo.GetTargets()[0]);
        if (!IsCanCast) MoveTo();
    }

    private void OnEnable() { Canceled += CancelMove; }
    private void OnDisable() { Canceled -= CancelMove; }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        while (Targeting.GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);
                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (!IsAllyTarget(character))
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
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character != null)
            CmdAddShield(Targeting.GetTarget().Character.gameObject);
        yield return null;
    }

    [Command]
    private void CmdAddShield(GameObject targetShield)
    {
        if (targetShield == null) return;

        var shield = Instantiate(_shieldPref, targetShield.transform.position, Quaternion.identity);
        NetworkServer.Spawn(shield.gameObject);
        _shield = shield.gameObject;
        ClientRpcShieldFollow(_shield, targetShield.transform);
        StartCoroutine(ShieldJob(_shield.gameObject));

        targetShield.GetComponent<Character>().CharacterState.AddState(
            States.CoolingDamaged, _baffDuration, 0, Hero.gameObject, name);
    }

    private IEnumerator ShieldJob(GameObject shield)
    {
        yield return new WaitForSeconds(_baffDuration);
        if (shield != null)
            NetworkServer.Destroy(shield);
    }

    [ClientRpc]
    private void ClientRpcShieldFollow(GameObject shield, Transform target)
    {
        shield.GetComponent<Shield>().FollowTo(target);
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Hero.Move.StopLookAt();
    }
}
