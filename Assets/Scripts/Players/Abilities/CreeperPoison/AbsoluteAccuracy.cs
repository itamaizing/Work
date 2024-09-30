using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AbsoluteAccuracy : Skill
{
    public float DecreaseCooldownTime = 2f;

    [Header("Talent")]
    [SerializeField] private AbsoluteAccuracyTalent _absoluteAccuracyTalent;
    [SerializeField] private KillersStamina _killersStamina;
    [SerializeField] private ColdBlood _coldBlood;

    [Header("Ability Properties")]
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private Character _player;

    private Character _target;
    private Vector3 _mousePosition = Vector3.positiveInfinity;

    private float _cooldownWithTalent = 4f;

    private bool _isPlayer = false;
    private bool _isCanCritCreeperStrike;
    private bool _isCanCritLightningStrikes;
    private bool _isCanCast = false;


    public bool IsCanCritCreeperStrike { get => _isCanCritCreeperStrike; set => _isCanCritCreeperStrike = value; }
    public bool IsCanCritLightningStrikes { get => _isCanCritLightningStrikes; set => _isCanCritLightningStrikes = value; }

    protected override bool IsCanCast { get { return _absoluteAccuracyTalent.IsActive; } }

    protected override void ClearData()
    {
        Debug.Log("AbsoluteAccuracy / ClearData");
        _isCanCast = false;
        _mousePosition = Vector3.positiveInfinity;
        _target = null;
        _isPlayer = false;
        if (_player.CharacterState.CheckForState(States.Immateriality))
        {
            _player.CharacterState.CmdRemoveState(States.Immateriality);
        }
    }

    protected override IEnumerator PrepareJob()
    {
        _player.CharacterState.CmdAddState(States.Immateriality, 0, 0, _player.gameObject, Name);

        Debug.Log("AbsoluteAccurcay / PrepareJob");
        if (_absoluteAccuracyTalent.IsActive)
        {
            Debug.Log("AbsoluteAccurcay / PrepareJob / first if == true");
            if (_coldBlood.IsActive)
            {
                Debug.Log("AbsoluteAccurcay / PrepareJob / second if == true");
                while (_target == null && float.IsPositiveInfinity(_mousePosition.x))
                {
                    Debug.Log("AbsoluteAccurcay / PrepareJob / while start");
                    if (Input.GetMouseButtonDown(0))
                    {
                        Debug.Log("AbsoluteAccurcay / PrepareJob / Input.GetMouseButtonDown");
                        _target = GetRaycastTarget(true);
                        Debug.Log("AbsoluteAccurcay / PrepareJob / Input.GetMouseButtonDown / target == " + _target);
                        _mousePosition = GetMousePoint();
                        Debug.Log("AbsoluteAccurcay / PrepareJob / Input.GetMouseButtonDown / _mousePosition == " + _mousePosition);

                        if (_target != _player)
                        {
                            _isPlayer = false;
                            Debug.Log("Target != player / Target == " + _target);
                        }
                        else if (_target == _player)
                        {
                            _isPlayer = true;
                            Debug.Log("Target == player / Target == " + _target);
                        }
                    }
                    yield return null;
                }
                Debug.Log("AbsoluteAccurcay / PrepareJob / after while");
                _isCanCast = true;
            }
            else
            {
                Debug.Log("AbsoluteAccurcay / PrepareJob / else");
                yield break;
            }
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_coldBlood.IsActive)
        {
            UseAbilityWithTalent();
        }
        else
        {
            UseAbilityWithoutTalent();
        }
        yield return null;
    }

    private void UseAbilityWithTalent()
    {
        if (_isPlayer)
        {
            IncreaseSetCooldown(_cooldownWithTalent);
            Debug.Log("AbsoluteAccuracy / UseAbilityWithTalent / if _isPlayer == true");
            _player.CharacterState.Dispel(StateType.Physical);
        }
        else if (!_isPlayer)
        {
            Debug.Log("AbsoluteAccuracy / UseAbilityWithTalent / else if _isPlayer == false");
            if (_killersStamina.IsActive)
            {
                _isCanCritLightningStrikes = true;
            }

            _isCanCritCreeperStrike = true;
        }
    }

    private void UseAbilityWithoutTalent()
    {
        Debug.Log("AbsoluteAccuracy / UseAbilityWithoutTalent");
        if (_killersStamina.IsActive)
        {
            _isCanCritLightningStrikes = true;
        }

        _isCanCritCreeperStrike = true;
    }
}
