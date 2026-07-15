using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Sleep : AbstractCharacterState
{
    public bool turnOff = false;
    private float _baseDuration;
    private bool _previousIsSelect;
    private int _initialLayer;
    private int _lastTickedSecond;
    private bool _giveInnerDarkness;
    private float _tickTimer;
    private const float _tickInterval = 1f;
    private const string _enemyLayerName = "Enemy";

    private byte _originalTeamIndex;

    private Character _source;
    private SkillManager _skillManager;
    private List<Skill> _disabledSkills = new List<Skill>();

    public override States State => States.Sleep;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _source = personWhoMadeBuff;
        _baseDuration = durationToExit;
        _giveInnerDarkness = false;

        _tickTimer = 0f;
        
        _lastTickedSecond = Mathf.CeilToInt(durationToExit); 

        _initialLayer = character.gameObject.layer;
        character.gameObject.layer = LayerMask.NameToLayer(_enemyLayerName);

        character.Character.Health.DamageTaken += OnAnyDamage;

        MoveComponent moveComponent = characterState.Character.Move;
        _skillManager = characterState.Character.Abilities;

        if (moveComponent != null)
        {
            _previousIsSelect = moveComponent.IsSelect;
            moveComponent.StopLookAt();
            moveComponent.IsSelect = false;
            moveComponent.IsMoving = false;
            moveComponent.Rigidbody.linearVelocity = Vector3.zero;
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
            var sleep = _source.Abilities.Abilities.OfType<SleepSpell>().FirstOrDefault();
            if (sleep != null) _giveInnerDarkness = sleep.IsSleepInnerDarknessTalentActive;
        }

        var networkSettings = characterState.Character.NetworkSettings;

        if (networkSettings != null && NetworkServer.active)
        {
            _originalTeamIndex = networkSettings.TeamIndex;
            networkSettings.TeamIndex = 3;
            networkSettings.RpcUpdateLayers();
        }
    }

    private void SubscribeOnDamage()
    {
        characterState.Character.Health.DamageTaken += OnDamaged;
        characterState.Character.Health.OnBeforeTakeDamage += OnDamaged;
    }

    private void UnSubscribeOnDamage()
    {
        characterState.Character.Health.DamageTaken -= OnDamaged;
        characterState.Character.Health.OnBeforeTakeDamage -= OnDamaged;
    }

    private void OnDamaged(Damage damage, Skill ability)
    {
        ExitState();
    }

    public override void GloabalUpdate()
    {
        if(duration >= 0 && duration != -1)
        {
            duration -= Time.deltaTime;

            if (_giveInnerDarkness)
            {
                int currentSecond = Mathf.CeilToInt(duration);
                
                if (currentSecond < _lastTickedSecond)
                {
                    CmdStateInnerDarkness();
                    _lastTickedSecond = currentSecond;
                }
            }

            if(duration <= 0)
            {
                ExitState();
            }
        }
    }
    
    public override void UpdateState()
    {
    }

    public override void ExitState()
    {
        characterState.gameObject.layer = _initialLayer;

        //if (_giveInnerDarkness) for (int i = 0; i < 3; i++) CmdStateInnerDarkness();

        MoveComponent moveComp = characterState.Character.Move;
        if (moveComp != null)
        {
            moveComp.IsSelect = _previousIsSelect;
            moveComp.IsMoving = false;
            moveComp.Rigidbody.linearVelocity = Vector3.zero;
            //moveComp.SetAnimationMovement(Vector3.zero);
        }

        foreach (var skill in _disabledSkills) skill.Disactive = false;

        characterState.Character.Health.DamageTaken -= OnAnyDamage;

        _disabledSkills.Clear();
        characterState.StateIcons.RemoveItemByState(State);
        characterState.RemoveState(this);

        var networkSettings = characterState.Character.NetworkSettings;

        if (networkSettings != null && NetworkServer.active)
        {
            networkSettings.TeamIndex = _originalTeamIndex;
            networkSettings.RpcUpdateLayers();
        }
    }

    public override bool Stack(float time)
    {
        duration = _baseDuration;
        return false;
    }

    private void OnAnyDamage(Damage damage, Skill fromSkill) => turnOff = true;

    [Command] private void CmdStateInnerDarkness() => ClientRpcStateInnerDarkness();
    [ClientRpc] private void ClientRpcStateInnerDarkness() { characterState.AddStateLogic(States.InnerDarkness, 13, 0f, Schools.None, _source.gameObject, null); }

    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        
        UnSubscribeOnDamage();
        SubscribeOnDamage();

        EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        return this;
    }
    

    //private bool ShouldApplyInnerDarkness()
    //{
    //    if (_personWhoMadeBuff == null || _personWhoMadeBuff.Abilities == null) return false;

    //    var song = _personWhoMadeBuff.Abilities.Abilities.OfType<SongOfSleep>().FirstOrDefault();
    //    return song != null && song.IsSleepInnerDarknessTalentActive;
    //}
}
