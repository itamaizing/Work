using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PassiveCombo_Scorpion : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private HeroComponent _hero;
    [SerializeField] private ComboPoints_Player _comboPlayer;

    //[SerializeField] private Sub_LavaPool_Scorpion _poolPrefab;

    [Header("Combo Settings")]
    private List<Skill> _usedSkills = new();
    [SerializeField] private float _comboTimeout = 1f;
    private Coroutine _comboTimerCoroutine;

    [HideInInspector]
    public Character CurrentTarget;
    [Header("Visuals")]
    [SerializeField] private ParticleSystem _particlesAddStack;
    ///[SerializeField] private ParticleSystem _particlesNoCharges;
    [SerializeField] private ParticleSystem _particlesFullCombo;
    [SerializeField] private ParticleSystem _particlesCancelCombo;

    [Header("Talents")]
    private bool _consumeComboTalent = false;
    private bool _multiTargetComboTalent = false;

    public void ConsumeComboTalent(bool value) => _consumeComboTalent = value;

    public void SetMultiTargetComboTalent(bool value) => _multiTargetComboTalent = value;
    
    #region Новый талант 3о — Импульсный огонь
    private ImpulseFireTalentBooster _impulseFireBooster;
    public ImpulseFireTalentBooster ImpulseFireBooster => _impulseFireBooster;
    #endregion

    #region Add Skill (Комбо механика)
    
    private void OnEnable()
    {
        _impulseFireBooster = new ImpulseFireTalentBooster(this);

        foreach (var skill in _hero.Abilities.Abilities)
        {
            if (skill is IComboParticipatingSkill)
            {
                IComboParticipatingSkill comboSkill = skill as IComboParticipatingSkill;
                comboSkill.OnDamaged += OnSkillDamageApplied;
            }
        }
    }

    private void OnDisable()
    {
        foreach (var skill in _hero.Abilities.Abilities)
        {
            if (skill is IComboParticipatingSkill)
            {
                IComboParticipatingSkill comboSkill = skill as IComboParticipatingSkill;
                comboSkill.OnDamaged -= OnSkillDamageApplied;
            }
        }
    }
    public void RegisterFireComboHit(Skill skill, Character target)
    {
        if (skill == null || target == null) return;
        CmdRegisterFireCombo(skill, target.gameObject);
    }
    
    /*private void OnSkillDamageApplied(GameObject targetGO, Skill skill)
    {
        if (!_consumeComboTalent && !_multiTargetComboTalent) return;
        if (targetGO == null) return;

        var target = targetGO.GetComponent<Character>();
        if (target == null) return;
        
        AddSkill(target, skill);
    }*/
    
    private void OnSkillDamageApplied(GameObject targetGO, Skill skill)
    {
        if (!_consumeComboTalent && !_multiTargetComboTalent) return;
        if (targetGO == null) return;

        var target = targetGO.GetComponent<Character>();
        if (target == null) return;

        AddSkillInternal(target, skill, isFireSkill: false);
    }
    
    [Command]
    private void CmdRegisterFireCombo(Skill skill, GameObject targetGO)
    {
        var target = targetGO?.GetComponent<Character>();
        if (target == null) return;
        AddSkillInternal(target, skill, isFireSkill: true);
    }

    private void AddSkillInternal(Character enemy, Skill skill, bool isFireSkill)
    {
        if(!isFireSkill)
            if (!_consumeComboTalent && !_multiTargetComboTalent) return;
        if (!isFireSkill && !_impulseFireBooster.CanUseInCombo(skill)) return;

        if (enemy == null || skill == null) return;

        var comboParticipating = skill as IComboParticipatingSkill;
        var fireComboParticipating = skill as IFireComboParticipatingSkill;

        int currentStacks = enemy.CharacterState.CheckStateStacks(States.ComboState);
        int maxStacks = enemy.CharacterState.GetState(States.ComboState)?.MaxStacksCount ?? int.MaxValue;

        if (CurrentTarget == null) CurrentTarget = enemy;

        if (!_multiTargetComboTalent && CurrentTarget != enemy)
        {
            ResetCounter();
            CurrentTarget = enemy;
        }

        _usedSkills.Add(skill);
        StartOrRestartComboTimer();

        if (_usedSkills.Count >= 3)
        {
            var lastThreeHits = _usedSkills.Skip(Mathf.Max(0, _usedSkills.Count - 3)).ToList();

            if (lastThreeHits.All(s => s == lastThreeHits[0]))
            {
                ResetCounter();
                return;
            }

            RpcPlayParticles("FullCombo");
            TryUseChargersOnLasts(enemy, lastThreeHits);

            if ((_consumeComboTalent || isFireSkill) && currentStacks < maxStacks)
                ApplyComboState(enemy);

            if (IsFinalComboSkill(enemy, skill))
            {
                comboParticipating?.OnFinalComboSkill(enemy.gameObject);
                fireComboParticipating?.OnFinalComboSkill(enemy.gameObject);
            }

            ResetCounter();
        }

        if (_comboPlayer != null && _comboPlayer.HasPoints())
        {
            int points = _comboPlayer.CurrentComboPoints;
            comboParticipating?.OnTargetHasComboPoint(enemy.gameObject, points);
            fireComboParticipating?.OnTargetHasComboPoint(enemy.gameObject,points);
            _comboPlayer.TryUse(points);
        }
    }

    private void TryUseChargersOnLasts(Character enemy, List<Skill> lastThreeHits)
    {
        if (_usedSkills.Count < 3) return;
        if (lastThreeHits.All(s => s == lastThreeHits[0])) return;

        var grouped = lastThreeHits.GroupBy(s => s)
            .ToDictionary(g => g.Key, g => g.Count());

        bool hasEnoughCharges = grouped.All(pair => pair.Key.Chargers >= pair.Value);

        if (hasEnoughCharges)
        {
            foreach (var pair in grouped)
                UseCharges(pair.Key, pair.Value);
        }
    }

    [ClientRpc]
    private void UseCharges(Skill skill, int amount)
    {
        if (skill == null) return;

        for (int i = 0; i < amount; i++)
        {
            skill.TryUseCharge();
        }
    }

    [ClientRpc]
    private void RpcPlayParticles(string type)
    {
        switch (type)
        {
            case "AddStack":
                _particlesAddStack?.Play();
                break;
            //case "NoCharges":
            //    _particlesNoCharges?.Play();
            //    break;
            case "FullCombo":
                _particlesFullCombo?.Play();
                break;
            case "Cancel":
                _particlesCancelCombo?.Play();
                break;
        }
    }

    #endregion

    #region Combo Timer

    private void StartOrRestartComboTimer()
    {
        if (_comboTimerCoroutine != null)
            StopCoroutine(_comboTimerCoroutine);

        _comboTimerCoroutine = StartCoroutine(ComboTimerCoroutine());
    }

    private IEnumerator ComboTimerCoroutine()
    {
        float timer = _comboTimeout;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        
        ResetCounter();
    }

    public void ResetCounter()
    {
        if (_comboTimerCoroutine != null)
        {
            StopCoroutine(_comboTimerCoroutine);
            _comboTimerCoroutine = null;
        }
        _usedSkills.Clear();
        CurrentTarget = null;
    }

    #endregion

    #region Debuff и ComboState

    private void ApplyComboState(Character enemy)
    {
        var consumeCombo = _hero.Abilities.GetSkill<ConsumeCombo_Scorpion>();
        if (consumeCombo == null)
        {
            return;
        }
        consumeCombo.ApplyComboEffect(enemy.transform);
    }

    private bool IsFinalComboSkill(Character target, Skill skill)
    {
        if (_usedSkills.Count < 3) return false;
        var lastThree = _usedSkills.Skip(_usedSkills.Count - 3).ToList();
        return lastThree.Last() == skill;
    }

    #endregion

    #region Network Commands

    private void SpawnLavaPool(Transform enemy)
    {
       /* GameObject pool = Instantiate(_poolPrefab.gameObject, enemy.transform.position, Quaternion.identity);
        pool.transform.rotation *= Quaternion.Euler(90f, 0f, 0f);

        SceneManager.MoveGameObjectToScene(pool, _hero.NetworkSettings.MyRoom);

        pool.GetComponent<Sub_LavaPool_Scorpion>().Init();
        NetworkServer.Spawn(pool);*/
    }

    #endregion
}
