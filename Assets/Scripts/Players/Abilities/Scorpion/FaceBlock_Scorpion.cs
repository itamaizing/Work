using System;
using System.Collections;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class FaceBlock_Scorpion : Skill
{
    [Header("Block Settings")]
    [SerializeField] private float _blockChance = 90f;
    [SerializeField] private float _blockReduction = 90f;
    [SerializeField] private float _blockAngle = 45f;

    private bool _isBlocking = false;
    
    private static readonly int _faceBlockTrigger = Animator.StringToHash("FaceBlock");

    protected override bool IsCanCast => CanCast();

    private bool CanCast()
    {
        return !_isBlocking;}
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        if (_hero?.Health != null)
            _hero.Health.OnBeforeDamage += OnBeforeTakeDamage;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo info = new TargetInfo();
        info.AddTarget(Hero);
        callbackDataSaved(info);
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        CmdSetIsBlocking();
        _hero.Move.SetCanMove(false);

        yield return null;
    }
    
    private void EndBlock()
    {
        _isBlocking = false;
    }

    [Command]
    private void CmdSetIsBlocking()
    {
        _isBlocking = true;
    }

    [TargetRpc]
    private void TargetSetAnimTrigger(GameObject target, int anim)
    {
        _hero.Animator.SetTrigger(anim);
    }

    private void OnDisable()
    {
        if (_hero?.Health != null)
            _hero.Health.OnBeforeDamage -= OnBeforeTakeDamage;
    }

    private void OnBeforeTakeDamage(ref Damage damage, Skill skill)
    {
        if (!_isBlocking || skill?.Hero == null) return;

        Character attacker = skill.Hero;

        if (damage.PhysicAttackType != AttackRangeType.MeleeAttack)
            return;

        Vector3 dirToAttacker = (attacker.transform.position - _hero.transform.position).normalized;
        float angle = Vector3.Angle(_hero.transform.forward, dirToAttacker);

        if (angle > _blockAngle / 2f)
            return;

        if (Random.value > _blockChance / 100f)
        {
            EndBlock();
            return;
        }

        float blockedAmount = damage.Value * (_blockReduction / 100f);
        damage.Value -= blockedAmount;

        _hero.Resources[ResourceType.Energy].TryUse(skill.Cost.BaseCost);
        EndBlock();
        _isBlocking = false;
        TargetSetAnimTrigger(_hero.gameObject,_faceBlockTrigger);
        _hero.Animator.SetTrigger(_faceBlockTrigger);
    }

    protected override void ClearData()
    {
    }
}