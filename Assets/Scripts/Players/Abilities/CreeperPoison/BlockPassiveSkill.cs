using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class BlockPassiveSkill : Skill, IPassiveSkill
{
    [SerializeField] private float durationWindowsBoost = 2f;
    [SerializeField] private float blockChance = 50;

    private Coroutine _boostWindow;
    private bool _isCooldownActive = false;
    private Character _attacker;
    //private Character _target;
    private List<Character> _validAttackers = new();

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

    private void OnEnable()
    {
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
        if (_boostWindow != null || _attacker == null) return;
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
        if (_isCooldownActive || _boostWindow != null || target == null) return;
        CmdAddAttacker(target);
        _boostWindow = StartCoroutine(BlockPassiveSkillBoostWindow());
    }

    private IEnumerator BlockPassiveSkillBoostWindow()
    {
        if (_boostWindow != null) StopCoroutine(_boostWindow);
        Hero.Health.CmdSetBlockChance(blockChance);
        _isCooldownActive = true;
        Hero.Health.BlockChance = blockChance;
        Disactive = false;

        yield return new WaitForSeconds(durationWindowsBoost);

        Hero.Health.CmdResetBlockChance();
        ResetDisactive();

        yield return new WaitForSeconds(6f);
        _isCooldownActive = false;
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
        ClientRpcResetDisactive();
    }

    private void ResetDisactive()
    {
        _attacker = null;
        Targeting.ClearTarget();
        //_target = null;
        Disactive = true;
        _boostWindow = null;
    }

    [ClientRpc] private void ClientRpcResetDisactive() => ResetDisactive();

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
        _validAttackers.Clear();
        _validAttackers.Add(target);
    }    
}