using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class BlockPassiveSkill : Skill, IPassiveSkill
{
    [SerializeField] private float durationWindowsBoost = 2f;
    [SerializeField] private float blockChance = 50;
    [SerializeField] private float cooldownPerTarget = 6f;

    private Dictionary<Character, Coroutine> _boostWindows = new();
    private Dictionary<Character, float> _cooldownEndTime = new();

    private Character _attacker;
    private HashSet<Character> _validAttackers = new();

    #region Skill
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override IEnumerator CastJob() => null;
    protected override void ClearData() { }
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) => null;
    #endregion

    #region Talent

    private bool _isMagicOrPhysicRessist = false;

    public void MagicOrPhysicRessist(bool value) => _isMagicOrPhysicRessist = value;

    #endregion

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        Hero.Health.Block += PlayBlockAnimation;
        Hero.Health.Evaded += OnHeroEvade;
        Hero.Health.OnBeforeTakeDamage += OnBeforeTakeDamage;

        Hero.Health.OnTryResist += TryResist;
    }

    private void OnDisable()
    {
        Hero.Health.Block -= PlayBlockAnimation;
        Hero.Health.Evaded -= OnHeroEvade;
        Hero.Health.OnBeforeTakeDamage -= OnBeforeTakeDamage;

        Hero.Health.OnTryResist -= TryResist;
    }

    private void OnHeroEvade()
    {
        if (_attacker == null) return;
        TargetRpcStartBlockPassiveSkillBoostWindow(connectionToClient, _attacker.netId);
    }

    private void OnBeforeTakeDamage(Damage damage, Skill skill)
    {
        if (skill == null || skill.Hero == null) return;

        _attacker = skill.Hero;
        if (!_validAttackers.Contains(_attacker)) Hero.Health.BlockChance = 0f;
    }

    public void TryStartBlockPassiveSkillBoostWindow(Character target)
    {
        if (target == null) return;
        if (_boostWindows.ContainsKey(target)) return;
        if (_cooldownEndTime.TryGetValue(target, out float endTime) && Time.time < endTime) return;

        CmdAddAttacker(target);
        _validAttackers.Add(target);
        Disactive = false;

        _boostWindows[target] = StartCoroutine(BlockPassiveSkillBoostWindow(target));
    }

    private IEnumerator BlockPassiveSkillBoostWindow(Character target)
    {
        Hero.Health.CmdSetBlockChance(blockChance);
        Hero.Health.BlockChance = blockChance;
        EnableSkillBoost();
        
        
        yield return new WaitForSeconds(durationWindowsBoost);

        EndBoostWindow(target);
    }

    private void EndBoostWindow(Character target)
    {
        _boostWindows.Remove(target);
        _validAttackers.Remove(target);
        _cooldownEndTime[target] = Time.time + cooldownPerTarget;
        DisableSkillBoost();
        if (_boostWindows.Count == 0)
        {
            Hero.Health.CmdResetBlockChance();
            Disactive = true;
        }
    }

    private bool TryResist(Damage damage)
    {
        if (!_isMagicOrPhysicRessist) return false;
        if (_attacker == null) return false;

        float chance = 50f;

        float roll = UnityEngine.Random.Range(0f, 100f);

        if (roll > chance) return false;

        switch (damage.Type)
        {
            case DamageType.Magical:
                Debug.Log("Magic resist triggered");
                return true;

            case DamageType.Physical:
                Debug.Log("Physical resist triggered");
                return true;
        }

        return false;
    }

    private void PlayBlockAnimation()
    {
        if (isServer) TargetRpcPlayBlockAnimation(Hero.connectionToClient);
        Hero.Health.ResetBlockChance();
        ClientRpcResetAllBoostWindows();
    }

    private void ResetAllBoostWindows()
    {
        foreach (var kvp in _boostWindows)
        {
            if (kvp.Value != null) StopCoroutine(kvp.Value);
        }

        _boostWindows.Clear();
        _validAttackers.Clear();
        _attacker = null;
        Targeting.ClearTarget();
        Disactive = true;
    }

    [ClientRpc] private void ClientRpcResetAllBoostWindows() => ResetAllBoostWindows();

    [TargetRpc]
    private void TargetRpcStartBlockPassiveSkillBoostWindow(NetworkConnection target, uint attackerNetId)
    {
        if (NetworkClient.spawned.TryGetValue(attackerNetId, out NetworkIdentity identity))
        {
            Character attacker = identity.GetComponent<Character>();
            if (attacker != null) TryStartBlockPassiveSkillBoostWindow(attacker);
        }
    }

    [TargetRpc] private void TargetRpcPlayBlockAnimation(NetworkConnection target) => Hero.Animator.SetTrigger(Animator.StringToHash("BlockTrigger"));

    [Command]
    private void CmdAddAttacker(Character target)
    {
        if (!_validAttackers.Contains(target)) _validAttackers.Add(target);
    }
}