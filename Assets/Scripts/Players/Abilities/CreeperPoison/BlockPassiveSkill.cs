using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class BlockPassiveSkill : Skill, IPassiveSkill
{
    [SerializeField] private float durationWindowsBoost = 1f;
    [SerializeField] private float blockChance = 50;
    [SerializeField] private float cooldownPerTarget = 6f;
    [SerializeField] private int meleeHitsToTrigger = 2;

    private Dictionary<Character, Coroutine> _boostWindows = new();
    private Dictionary<Character, float> _cooldownEndTime = new();
    private Dictionary<Character, int> _meleeHitCounts = new();

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

        Hero.Health.OnTryResist += TryResist;
    }

    private void OnDisable()
    {
        Hero.Health.Block -= PlayBlockAnimation;
        Hero.Health.Evaded -= OnHeroEvade;

        Hero.Health.OnTryResist -= TryResist;
    }

    private void OnHeroEvade(Skill skill)
    {
        Character attacker = skill?.Hero as Character;
        if (attacker == null) return;

        TargetRpcStartBlockPassiveSkillBoostWindow(connectionToClient, attacker.netId);
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
        EnableSkillBoost();

        yield return new WaitForSeconds(durationWindowsBoost);

        EndBoostWindow(target);
    }

    private void EndBoostWindow(Character target)
    {
        _boostWindows.Remove(target);
        _validAttackers.Remove(target);
        CmdRemoveAttacker(target);
        _meleeHitCounts.Remove(target);
        _cooldownEndTime[target] = Time.time + cooldownPerTarget;
        DisableSkillBoost();
        if (_boostWindows.Count == 0)
        {
            Disactive = true;
        }
    }
    
    private bool TryResist(Damage damage, Skill skill)
    {
        if (IsDot(damage)) return false;

        Character attacker = skill?.Hero as Character;

        if (TryBlock(damage, attacker)) return true;
        if (TryMagicOrPhysicResist(damage, attacker)) return true;

        return false;
    }

    private static bool IsDot(Damage damage) =>
        damage.Type == DamageType.DOTPhys || damage.Type == DamageType.DOTMag;

    private bool TryBlock(Damage damage, Character attacker)
    {
        if (damage.Type != DamageType.Physical) return false;
        if (attacker == null) return false;
        if (!_validAttackers.Contains(attacker)) return false;

        if (UnityEngine.Random.Range(0f, 100f) > blockChance) return false;

        Hero.Health.InvokeBlock();
        return true;
    }

    private bool TryMagicOrPhysicResist(Damage damage, Character attacker)
    {
        if (!_isMagicOrPhysicRessist) return false;
        if (attacker == null) return false;

        float chance = 50f;

        if (UnityEngine.Random.Range(0f, 100f) > chance) return false;

        switch (damage.Type)
        {
            case DamageType.Magical:
                return true;

            case DamageType.Physical:
                return true;
        }

        return false;
    }

    private void PlayBlockAnimation()
    {
        if (isServer) TargetRpcPlayBlockAnimation(Hero.connectionToClient);
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
        CmdClearAttackers();
        _meleeHitCounts.Clear();
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

    [Command]
    private void CmdRemoveAttacker(Character target)
    {
        if (_validAttackers.Contains(target)) _validAttackers.Remove(target);
    }

    [Command]
    private void CmdClearAttackers()
    {
        _validAttackers.Clear();
    }
}