using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Sleep : AbstractCharacterState
{
    private float _duration;
    private float _baseDuration;
    private bool _previousIsSelect;
    private int _initialLayer;
    private bool _giveInnerDarkness;
    private const string _enemyLayerName = "Enemy";

    private Character _source;
    private SkillManager _skillManager;
    private List<Skill> _disabledSkills = new List<Skill>();

    public override States State => States.Sleep;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Жертва погружена в сон");

        _characterState = character;
        _source = personWhoMadeBuff;
        _duration = durationToExit;
        _baseDuration = durationToExit;
        _giveInnerDarkness = false;

        _initialLayer = character.gameObject.layer;
        character.gameObject.layer = LayerMask.NameToLayer(_enemyLayerName);

        character.Character.Health.DamageTaken += OnAnyDamage;

        MoveComponent moveComponent = _characterState.Character.Move;
        _skillManager = _characterState.Character.Abilities;

        if (moveComponent != null)
        {
            _previousIsSelect = moveComponent.IsSelect;
            moveComponent.StopLookAt();
            moveComponent.IsSelect = false;
            moveComponent.IsMoving = false;
            moveComponent.Rigidbody.velocity = Vector3.zero;
            //moveComponent.SetAnimationMovement(Vector3.zero);
        }

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

        if (_source != null && _source.Abilities != null)
        {
            var song = _source.Abilities.Abilities.OfType<SongOfSleep>().FirstOrDefault();
            if (song != null) _giveInnerDarkness = song.IsSleepInnerDarknessTalentActive;
        }
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;

        if (_duration <= 0) ExitState();
    }

    public override void ExitState()
    {
        Debug.Log("Эффект сна закончился");

        _characterState.gameObject.layer = _initialLayer;

        if (_giveInnerDarkness) for (int i = 0; i < 3; i++) CmdStateInnerDarkness();

        MoveComponent moveComp = _characterState.Character.Move;
        if (moveComp != null)
        {
            moveComp.IsSelect = _previousIsSelect;
            moveComp.IsMoving = false;
            moveComp.Rigidbody.velocity = Vector3.zero;
            //moveComp.SetAnimationMovement(Vector3.zero);
        }

        foreach (var skill in _disabledSkills) skill.Disactive = false;

        _characterState.Character.Health.DamageTaken -= OnAnyDamage;

        _disabledSkills.Clear();
        _characterState.StateIcons.RemoveItemByState(State);
        _characterState.RemoveState(this);

    }

    public override bool Stack(float time)
    {
        _duration = _baseDuration;
        return false;
    }

    private void OnAnyDamage(Damage damage, Skill fromSkill) => ExitState();

    [Command] private void CmdStateInnerDarkness() => ClientRpcStateInnerDarkness();
    [ClientRpc] private void ClientRpcStateInnerDarkness() { _characterState.AddStateLogic(States.InnerDarkness, 13, 0f, Schools.None, _source.gameObject, null); }


    private bool ShouldApplyInnerDarkness()
    {
        if (_personWhoMadeBuff == null || _personWhoMadeBuff.Abilities == null) return false;

        var song = _personWhoMadeBuff.Abilities.Abilities.OfType<SongOfSleep>().FirstOrDefault();
        return song != null && song.IsSleepInnerDarknessTalentActive;
    }
}
