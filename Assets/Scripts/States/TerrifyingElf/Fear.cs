using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Fear : AbstractCharacterState
{
    private float _duration;
    private float _baseDuration;
    private Character _source;
    private bool _previousIsSelect;
    private Coroutine _moveCoroutine;
    private SkillManager _skillManager;
    private List<Skill> _disabledSkills = new List<Skill>();

    public override States State => States.Fear;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Чувство страха наложено");

        _characterState = character;
        _source = personWhoMadeBuff;
        _duration = durationToExit;
        _baseDuration = durationToExit;

        MoveComponent moveComponent = _characterState.Character.Move;
        _skillManager = _characterState.Character.Abilities;

        if (moveComponent != null)
        {
            _previousIsSelect = moveComponent.IsSelect;

            moveComponent.StopLookAt();
            moveComponent.IsSelect = false;
            moveComponent.IsMoving = true;

            if (_moveCoroutine != null)
            {
                _characterState.StopCoroutine(_moveCoroutine);
            }
            _moveCoroutine = _characterState.StartCoroutine(MoveAwayCoroutine(moveComponent));
        }
    }

    public override void UpdateState()
    {
        if (_skillManager != null)
        {
            foreach (var skill in _skillManager.Abilities)
            {
                if (!skill.Disactive)
                {
                    skill.Disactive = true;
                    _disabledSkills.Add(skill);
                }
            }
        }

        _duration -= Time.deltaTime;
        if (_duration <= 0f)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        Debug.Log("Эффект страха заканчивается");

        if (_moveCoroutine != null)
        {
            _characterState.StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        MoveComponent moveComp = _characterState.Character.Move;
        if (moveComp != null)
        {
            moveComp.IsSelect = _previousIsSelect;
            moveComp.IsMoving = false;
            moveComp.ExternalMoveDirection = Vector3.zero;
            moveComp.SetDefaultSpeed();
            moveComp.StopLookAt();
            moveComp.Rigidbody.velocity = Vector3.zero;
            //moveComp.SetAnimationMovement(Vector3.zero);
        }

        foreach (var skill in _disabledSkills)
        {
            skill.Disactive = false;
        }
        _disabledSkills.Clear();

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount == MaxStacksCount)
        {
            _duration = _baseDuration;
            return false;
        }
        return false;
    }

    private IEnumerator MoveAwayCoroutine(MoveComponent moveComp)
    {
        if (_source == null || _characterState == null || moveComp == null) yield break;

        Rigidbody rb = moveComp.Rigidbody;
        if (rb == null) yield break;

        //float targetDistance = 5f;
        Vector3 fleeDirection = (moveComp.transform.position - _source.transform.position).normalized;
        fleeDirection = Quaternion.Euler(0, Random.Range(-45f, 45f), 0) * fleeDirection;

        while (_duration > 0f)
        {
            yield return new WaitForSeconds(0.1f);

            //float distance = Vector3.Distance(_source.transform.position, moveComp.transform.position);

            //if (distance >= targetDistance)
            //{
            //    moveComp.SetAnimationMovement(Vector3.zero);
            //    moveComp.Rigidbody.velocity = Vector3.zero;
            //    yield break;
            //}

            //moveComp.SetAnimationMovement(Vector3.zero);
            moveComp.Rigidbody.velocity = Vector3.zero;

            if (Random.value <= 0.2f)
            {
                fleeDirection = (moveComp.transform.position - _source.transform.position).normalized;
                fleeDirection = Quaternion.Euler(0, Random.Range(-45f, 45f), 0) * fleeDirection;
            }

            moveComp.transform.DORotateQuaternion(Quaternion.LookRotation(fleeDirection), 0.2f);
            rb.velocity = fleeDirection * moveComp.CurrentSpeed;
            //moveComp.SetAnimationMovement(rb.velocity);
        }
    }
}
