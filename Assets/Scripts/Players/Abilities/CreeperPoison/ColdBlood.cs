using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ColdBlood : Skill
{
    [Header("Talent")]
    [SerializeField] private ColdBloodEnabledTalent _coldBloodEnabledTalent;
    [SerializeField] private ColdBloodTalent _coldBloodTalent;
    [SerializeField] private KillersStamina _killersStamina;

    [Header("Ability Properties")]
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private Character _player;

    private Character _target;
    private Vector3 _mousePosition = Vector3.positiveInfinity;

    private float _cooldownTimeWithTalent = 4f;
    private float _decreaseCooldownTime = 2f;

    private bool _isPlayer = false;
    private bool _isCanCritCreeperStrike;
    private bool _isCanCritLightningStrikes;
    private bool _isCanCast = false;

    private Coroutine _waitingHitFromCreeperStrike;
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    public bool IsCanCritCreeperStrike { get => _isCanCritCreeperStrike; set => _isCanCritCreeperStrike = value; }
    public bool IsCanCritLightningStrikes { get => _isCanCritLightningStrikes; set => _isCanCritLightningStrikes = value; }

    protected override bool IsCanCast { get { return _coldBloodEnabledTalent.Data.IsOpen; } }

    protected override void ClearData()
    {
        Debug.Log("ColdBlood / ClearData");
        _isCanCast = false;
        _mousePosition = Vector3.positiveInfinity;
        _target = null;
        _isPlayer = false;

        if (_player.CharacterState.CheckForState(States.Immateriality))
        {
            _player.CharacterState.CmdRemoveState(States.Immateriality);
        }

        if (_waitingHitFromCreeperStrike != null)
        {
            StopCoroutine(_waitingHitFromCreeperStrike);
            _waitingHitFromCreeperStrike = null;
        }
    }

    protected override IEnumerator PrepareJob()
    {
        if (_coldBloodTalent.Data.IsOpen)
        {
            while (_target == null && float.IsPositiveInfinity(_mousePosition.x))
            {
                if (GetMouseButton)
                {
                    _target = GetRaycastTarget(true);
                    Debug.Log("ColdBlood / PrepareJob / Input.GetMouseButtonDown / target == " + _target);

                    if (_target != _player)
                    {
                        _isPlayer = false;
                        Debug.Log("Target != player / Target == " + _target);
                    }
                    if (_target == _player)
                    { 
                        _isPlayer = true;
                        Debug.Log("Target == player / Target == " + _target);
                    }

                    _mousePosition = GetMousePoint();
                    Debug.Log("ColdBlood / PrepareJob / Input.GetMouseButtonDown / _mousePosition == " + _mousePosition);

                    _player.CharacterState.CmdAddState(States.Immateriality, 0, 0, _player.gameObject, Name);
                }
                yield return null;
            }
            _isCanCast = true;
        }
        else
        {
            yield break;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_coldBloodTalent.Data.IsOpen)
        {
            UseAbilityWithTalent();
        }
        else
        {
            UseAbilityWithoutTalent();
        }
        yield return _waitingHitFromCreeperStrike = StartCoroutine(WaitingHitFromCreeperStrikeJob());
    }

    public void ReducingAbilityCooldown()
    {
        float reducingMultiplier = 2f;
        ReductionSetCooldown(CooldownTime / reducingMultiplier);
    }

    private IEnumerator WaitingHitFromCreeperStrikeJob()
    {
        while (!_creeperStrike.IsHit)
        {
            yield return null;    
        }
    }

    private void UseAbilityWithTalent()
    {
        if (_isPlayer)
        {
            ReductionSetCooldown(_cooldownTimeWithTalent);
            Debug.Log("ColdBlood / UseAbilityWithTalent / if _isPlayer == true");
            _player.CharacterState.DispelStates(StateType.Physical, _target.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, true);
        }
        else
        {
            Debug.Log("ColdBlood / UseAbilityWithTalent / else if _isPlayer == false");
            if (_killersStamina.Data.IsOpen)
            {
                _isCanCritLightningStrikes = true;
            }

            _isCanCritCreeperStrike = true;
        }
    }

    private void UseAbilityWithoutTalent()
    {
        Debug.Log("ColdBlood / UseAbilityWithoutTalent");
        if (_killersStamina.Data.IsOpen)
        {
            _isCanCritLightningStrikes = true;
        }

        _isCanCritCreeperStrike = true;
    }
}
