using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class PortalDarkness : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float _duration = 3f;

    [Header("Energy Cost")]
    [SerializeField] private float _energyCost = 20f;

    private Energy _energy;

    #region ExplodingCorpseTalent

    private bool _isCorpseExploding;
    public bool IsCorpseExploding => _isCorpseExploding;

    public void EnableExplodingCorpse(bool value)
    {
        if(value == _isCorpseExploding) return;
        _isCorpseExploding = value;
        CmdEnableExplodingCorpse(_isCorpseExploding);
        if (_isCorpseExploding)
        {
            _hero.SpawnComponent.UnitAdded -= OnUnitSpawned;
            _hero.SpawnComponent.UnitAdded += OnUnitSpawned;
        }
        else
        {
            _hero.SpawnComponent.UnitAdded -= OnUnitSpawned;
        }
    }

    [Command]
    private void CmdEnableExplodingCorpse(bool value)
    {
        _isCorpseExploding = value;

    }

    private void OnUnitSpawned(Character minionCharacter)
    {
        if(minionCharacter == null) return;
        minionCharacter.Abilities.GetSkill<ExplodingCorpse>().OnCreatureSpawned();
    }

    #endregion

    protected override bool IsCanCast => Targeting.GetTarget() != null && Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private void EnsureEnergy()
    {
        if (_energy == null) _energy = (Energy)Hero.Resources[ResourceType.Energy];
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Targetable == null && !_disactive)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), 0.5f);

                var temp = Targeting.GetTempTarget()?.Targetable as Character;

                if (temp is IceDeadMinion)
                {
                    Targeting.ClearTempTarget();
                }
                else if(temp != null)
                {
                    Targeting.SetTarget(temp);
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

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;

        EnsureEnergy();

        float cost = Buff.ManaCost.GetBuffedValue(_energyCost);

        if (!Cost.TryPaySingle(cost, ResourceType.Energy, shouldModify: false))
        {
            TryCancel(true);
            yield break;
        }

        if (target == null) yield break;

        CmdApplyDarkness(target.gameObject);
        AfterCastJob();
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
    }

    [Command]
    private void CmdApplyDarkness(GameObject targetObject)
    {
        var target = targetObject.GetComponent<Character>();
        if (target == null) return;
        
        target.CharacterState.AddState(States.PortalDarkness, _duration, 0, _playerLinks.gameObject, nameof(PortalDarkness));
    }


    [Command]
    public void CmdApplyPlague(GameObject targetObject,float duration)
    {
        var target = targetObject.GetComponent<Character>();
        if (target == null) return;
        
        target.CharacterState.AddState(States.Plague, duration, 0, _hero.gameObject, nameof(PortalDarkness));
    }
    
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
        {
            Targeting.SetTarget((ITargetable)(targetInfo.GetTargets()[0] as Character));
        }
    }
}