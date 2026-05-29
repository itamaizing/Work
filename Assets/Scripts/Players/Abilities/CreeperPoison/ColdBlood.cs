using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class ColdBlood : Skill
{
    //[Header("Talent")]
    //[SerializeField] private Indomitable _indomitable;
    //[SerializeField] private ColdBloodTalent _coldBloodTalent;
    //[SerializeField] private KillersStamina _killersStamina;

    [Header("Ability Properties")]
    [SerializeField] private CreeperStrike _creeperStrike;
   // [SerializeField] private Character _player;
    [SerializeField] private float _reducingCooldownMultiplier = 2f;

    //private Character _target;
    private Vector3 _mousePosition = Vector3.positiveInfinity;

    private float _cooldownTimeWithTalent = 4f;

    private bool _isPlayer = false;
    private bool _isCanCrit;
    private bool _isCanCritLightningStrikes;

    private bool _isWaitingForHit = false;

   // public Indomitable IndomitableTalent { get => _indomitable; }
    //public ColdBloodTalent ColdBloodTalent { get => _coldBloodTalent; }
    //public KillersStamina KillersStaminaTalent { get => _killersStamina; }
    public bool IsCanCrit { get => _isCanCrit; set => _isCanCrit = value; }
    public bool IsCanCritLightningStrikes { get => _isCanCritLightningStrikes; set => _isCanCritLightningStrikes = value; }

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => !_isWaitingForHit;

    protected override void Awake()
    {
        base.Awake();

        //_baseCooldownTime = Cooldown.BaseCooldownTime;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Debug.LogError("DataError");
    }

    protected override void ClearData()
    {
        Debug.Log("ColdBlood / ClearData");
        _mousePosition = Vector3.positiveInfinity;
        //_target = null;
        _isPlayer = false;

        if (Hero.CharacterState.CheckForState(States.Immateriality))
        {
			Hero.CharacterState.CmdRemoveState(States.Immateriality);
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
     //   if (_indomitable.Data.IsOpen)
     //   {
     //       while (Targeting.GetTarget()?.Character == null || float.IsPositiveInfinity(_mousePosition.x))
     //       {
     //           if (GetMouseButton)
     //           {
     //               Targeting.FindTempTarget(true);
					////_target = Targeting.GetTarget(true).character;
     //              // Debug.Log("ColdBlood / PrepareJob / Input.GetMouseButtonDown / target == " + _target);

     //               if (Targeting.GetTarget()?.Character != Hero)
     //               {
     //                   _isPlayer = false;
     //                  // Debug.Log("TargetLayer != player / TargetLayer == " + _target);
     //               }
     //               if (Targeting.GetTarget()?.Character == Hero)
     //               { 
     //                   _isPlayer = true;
     //                  // Debug.Log("TargetLayer == player / TargetLayer == " + _target);
     //               }

     //               _mousePosition = Targeting.GetMousePoint();
     //               Debug.Log("ColdBlood / PrepareJob / Input.GetMouseButtonDown / _mousePosition == " + _mousePosition);
     //           }
     //           yield return null;
     //       }
     //   }

        //else
        //{
        //    yield break;
        //}

        yield break;
    }

    protected override IEnumerator CastJob()
    {
        //if (_indomitable.Data.IsOpen)
        //{
        //    UseAbilityWithTalent();
        //}
        //else
        //{
        //    UseAbilityWithoutTalent();
        //}

        UseAbilityWithoutTalent();

        CmdApplyImmateriality();
        StartWaitingForHit();

        yield return null;
    }

    public void ReducingAbilityCooldown()
    { 
        if (Cooldown.RemainingTime > 0)
        {
            float reducingMultiplier = _reducingCooldownMultiplier;
            float newCooldownTime = Cooldown.RemainingTime / reducingMultiplier;
            Cooldown.SetReduced(newCooldownTime, shouldModify: false);
        }
        else
        {
            float reducingMultiplier = _reducingCooldownMultiplier;
            Cooldown.CooldownTime /= reducingMultiplier;
        }

        if (Hero.CharacterState.CheckForState(States.Immateriality))
        {
            Hero.CharacterState.CmdRemoveState(States.Immateriality);
        }

        if (Hero.CharacterState.CheckForState(States.Immateriality))
        {
            Hero.CharacterState.CmdRemoveState(States.Immateriality);
        }
    }

    private void StartWaitingForHit()
    {
        if (_isWaitingForHit) return;

        _isWaitingForHit = true;

        _creeperStrike.OnHit += OnCreeperStrikeHit;
    }

    private void OnCreeperStrikeHit()
    {
        _creeperStrike.OnHit -= OnCreeperStrikeHit;

        _isWaitingForHit = false;

        ReducingAbilityCooldown();
    }

    private void UseAbilityWithTalent()
    {
        if (_isPlayer)
        {
            Cooldown.SetReduced(_cooldownTimeWithTalent, shouldModify: true);
            Debug.Log("ColdBlood / UseAbilityWithTalent / if _isPlayer == true");
			Hero.CharacterState.DispelStates(StateType.Physical, Targeting.GetTarget().Character.NetworkSettings.TeamIndex, Hero.NetworkSettings.TeamIndex, true);
        }
        else
        {
            Debug.Log("ColdBlood / UseAbilityWithTalent / else if _isPlayer == false");
            //if (_killersStamina.Data.IsOpen)
            //{
            //    _isCanCritLightningStrikes = true;
            //}

            _isCanCrit = true;
        }
    }

    private void UseAbilityWithoutTalent()
    {
        Debug.Log("ColdBlood / UseAbilityWithoutTalent");
        //if (_killersStamina.Data.IsOpen)
        //{
        //    _isCanCritLightningStrikes = true;
        //}

        _isCanCrit = true;
    }

    [Command]
    private void CmdApplyImmateriality()
    {
        Hero.CharacterState.AddState(States.Immateriality, 999, 0, Hero.gameObject, Name);
    }
}
