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

    private Dictionary<Character, Coroutine> _boostWindows = new();
    private Dictionary<Character, double> _cooldownEndTime = new();
    private readonly Dictionary<Character, double> _boostEndTime = new();

    private HashSet<Character> _validAttackers = new();

    #region Skill
    protected override int AnimTriggerPrepare => 0;
    protected override int AnimTriggerCast => 0;
    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override IEnumerator CastJob() => null;
    protected override void ClearData() { }
    protected override IEnumerator TargetingJob(Action<TargetInfo> targetDataSavedCallback) => null;
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
        StopAllCoroutines();
        _boostWindows.Clear();
        _boostEndTime.Clear();
        _validAttackers.Clear();
        if (Hero == null || Hero.Health == null) return;

        Hero.Health.Block -= PlayBlockAnimation;
        Hero.Health.Evaded -= OnHeroEvade;

        Hero.Health.OnTryResist -= TryResist;
    }

    private void OnHeroEvade(Skill skill)
    {
        if (!isServer || !IsSkillActive) return;

        Character attacker = skill?.Hero as Character;
        if (attacker == null) return;

        TryStartBlockPassiveSkillBoostWindow(attacker);
    }

    [Server]
    public void TryStartBlockPassiveSkillBoostWindow(Character target)
    {
        if (!isServer || !IsSkillActive) return;
        if (target == null) return;
        if (_boostWindows.ContainsKey(target)) return;
        if (_cooldownEndTime.TryGetValue(target, out double endTime) && NetworkTime.time < endTime) return;

        _validAttackers.Add(target);
        _boostEndTime[target] = NetworkTime.time + durationWindowsBoost;
        if (_validAttackers.Count == 1 && connectionToClient != null)
            TargetRpcSetBlockBoost(connectionToClient, true);

        _boostWindows[target] = StartCoroutine(BlockPassiveSkillBoostWindow(target));
    }

    private IEnumerator BlockPassiveSkillBoostWindow(Character target)
    {
        yield return new WaitForSeconds(durationWindowsBoost);

        EndBoostWindow(target);
    }

    private void EndBoostWindow(Character target)
    {
        if (!_validAttackers.Remove(target)) return;
        _boostWindows.Remove(target);
        _boostEndTime.Remove(target);
        _cooldownEndTime[target] = NetworkTime.time + cooldownPerTarget;
        if (connectionToClient != null)
        {
            TargetRpcShowBlockCooldown(connectionToClient, target.netId, _cooldownEndTime[target]);
            if (_validAttackers.Count == 0) TargetRpcSetBlockBoost(connectionToClient, false);
        }
    }
    
    private bool TryResist(Damage damage, Skill skill)
    {
        if (!isServer || IsDot(damage)) return false;
        
        Character attacker = skill?.Hero as Character;

        if (TryBlock(damage, attacker)) return true;

        return false;
    }

    private static bool IsDot(Damage damage) => damage.IsDot;

    private bool TryBlock(Damage damage, Character attacker)
    {
        if (!IsSkillActive) return false;
        if (damage.Type != DamageType.Physical &&
            !(damage.Type == DamageType.Magical && _isMagicOrPhysicRessist)) return false;
        if (attacker == null) return false;
        if (!_validAttackers.Contains(attacker)) return false;
        if (!_boostEndTime.TryGetValue(attacker, out double endTime) || NetworkTime.time >= endTime) return false;

        if (UnityEngine.Random.Range(0f, 100f) > blockChance) return false;

        if (_boostWindows.TryGetValue(attacker, out Coroutine coroutine) && coroutine != null)
            StopCoroutine(coroutine);

        Hero.Health.InvokeBlock();
        EndBoostWindow(attacker);

        return true;
    }

    private void PlayBlockAnimation()
    {
        if (isServer) 
        {
            ClientRpcPlayBlockAnimation();
        }
    }
    
    [ClientRpc]
    private void ClientRpcPlayBlockAnimation()
    {
        Hero.Animator.SetTrigger(Animator.StringToHash("BlockTrigger"));
    }

    [TargetRpc]
    private void TargetRpcSetBlockBoost(NetworkConnection target, bool active)
    {
        Disactive = !active;
        if (active) EnableSkillBoost();
        else DisableSkillBoost();
    }

    [TargetRpc]
    private void TargetRpcShowBlockCooldown(NetworkConnection targetConnection, uint targetNetId, double endTime)
    {
        if (!NetworkClient.spawned.TryGetValue(targetNetId, out NetworkIdentity identity)) return;
        Character target = identity.GetComponent<Character>();
        if (target == null || target.CharacterState == null) return;

        float remaining = (float)(endTime - NetworkTime.time);
        var state = new LocalVisualState(States.BlockCooldown, endTime,
            "Блокировка удара: перезарядка", "Блокировка удара против этой цели перезаряжается.", Icon);
        target.CharacterState.AddLocalVisualState(state, remaining, Hero);
    }
}
