using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;

public class PhysicalAttack : Skill
{
    [Header("Auto-Attack settings")]
    [SerializeField] private float _attackZoneSize = .2f;
    [SerializeField] private float _attackDelay = .4f;
    [SerializeField] private float _chargeAttackDelay = 0f;

    public float AttackDelay => Buff.AttackSpeed.GetBuffedValue(_attackDelay);
    public Character Target => _target;
    public Vector2 LastTargetPosition => _lastTargetPosition;
    public bool IsAutoattackMode => _isAutoattackMode;
    public override bool IsPayCostStartCooldown => false;

    [Header("Damage / Combo")]
    [SerializeField] private HeroComponent _playerLinks;
    [SerializeField] private SeriesOfStrikes _combo;
    [SerializeField] private AudioClip[] Hits;

    private AudioSource _audio;
    private Animator _anim;
    private Energy _energy;
    private RuneComponent _rune;

    private Character _target;
    private Character _curTarget;
    private bool _isAutoattackMode = true;
    private Coroutine _autoAttackJob;
    private bool _isAttacking;
    private Vector2 _lastTargetPosition;

    private Vector2 _jumpPos;
    private float _multiplier = 1f;
    private bool _talentActive = false;
    private bool _rollingPhysTalent;
    private bool _seriesPhysicalTalent;
    private float _stunCount = 0f;

    private bool _rightKick = true;

    private static readonly int RightKickTrigger = Animator.StringToHash("RightKick");
    private static readonly int LeftKickTrigger = Animator.StringToHash("LeftKick");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast =>
        _target != null &&
        NoObstacles(_target.transform.position, _obstacle) &&
        IsTargetInRadius(Radius, _target.transform);

    private void Start()
    {
        _audio = GetComponent<AudioSource>();
        _anim = GetComponent<Animator>();

        _energy = (Energy)Hero.Resources.FirstOrDefault(r => r.Type == ResourceType.Energy);
        _rune = (RuneComponent)Hero.Resources.FirstOrDefault(r => r.Type == ResourceType.Rune);
    }

    private void Update()
    {
        if (_target == null) return;

        Color c0 = _isAttacking ? Color.green : Color.red;
        Color c1 = new Color(c0.r, c0.g, c0.b, 0f);
        float t = Mathf.PingPong(Time.time, 1f);
        _skillRender.DrawRadiusColor(Radius, Color.Lerp(c0, c1, t));
    }

    protected override IEnumerator PrepareJob(System.Action<TargetInfo> saved)
    {
        while (_target == null && !_disactive)
        {
            if (GetMouseButton)
            {
                var click = GetRaycastTarget();
                if (click != null)
                {
                    _target = click;
                    _target.SelectedCircle.IsActive = true;
                }
            }
            yield return null;
        }

        if (_target) Hero.Move.LookAtTransform(_target.transform);

        var targetInfo = new TargetInfo();
        targetInfo.Targets.Add(_target);
        saved?.Invoke(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null) yield break;

        _isAttacking = true;
        PlayAttackAnimation();

        yield return new WaitUntil(() => !_anim.IsInTransition(0) && _anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);

        ClearData();
    }

    private void PlayAttackAnimation()
    {
        _rightKick = !_rightKick;
        _anim.SetTrigger(_rightKick ? RightKickTrigger : LeftKickTrigger);
        CmdPlayHitSfx();
    }

    public void ApplyAttackDamage()
    {
        if (_target == null) return;

        if (_seriesPhysicalTalent) HitCombo(_target);
        else HitSingle(_target);
    }

    private void HitCombo(Character enemy)
    {
        if (_curTarget == enemy && _energy.CurrentValue >= 5f)
        {
            Buff.AttackSpeed.IncreasePercentage(_multiplier);

            float curDamage = _damageValue + Random.Range(0, 2);

            if (_combo.MakeHit(enemy, AbilityForm.Physical, 0, 5, curDamage))
            {
                LastHit();
            }

            _multiplier = 1 + _combo.GetMultipliedSpeed() / 100f;
            Buff.AttackSpeed.ReductionPercentage(_multiplier);

            ApplyDamageAndResource(enemy, curDamage, consumeEnergy: 5);

            if (_rollingPhysTalent)
                CmdState(enemy.gameObject, 0.7f * _stunCount);
        }
        else
        {
            Buff.AttackSpeed.IncreasePercentage(_multiplier);
            _multiplier = 1f;
            _curTarget = enemy;

            float dmg = _damageValue + Random.Range(0, 2);
            _combo.MakeHit(enemy, AbilityForm.Physical, 0, 0, dmg);

            bool spent = _energy.CurrentValue >= 5f;
            if (spent)
            {
                _energy.CmdUse(5);
                _multiplier = 1 + _combo.GetMultipliedSpeed() / 100f;
                Buff.AttackSpeed.ReductionPercentage(_multiplier);
            }

            ApplyDamageAndResource(enemy, dmg, consumeEnergy: spent ? 0 : 0);
        }

        if (Random.Range(0, 100) < 2 && _talentActive)
            _rune.CmdAdd(1);
    }

    private void LastHit()
    {
        if (_energy.CurrentValue < 10f) return;

        float extra = _damageValue * 0.5f;
        _energy.CmdUse(10);
        ApplyDamageAndResource(_curTarget, extra, consumeEnergy: 0);
        CmdState(_curTarget.gameObject, 1.5f);
        PushBackEnemy(_curTarget);
        _curTarget = null;
    }

    private void HitSingle(Character enemy)
    {
        float dmg = _damageValue + Random.Range(0, 2);
        ApplyDamageAndResource(enemy, dmg, consumeEnergy: 0);
    }

    private void ApplyDamageAndResource(Character enemy, float dmg, float consumeEnergy)
    {
        if (consumeEnergy > 0) _energy.CmdUse(consumeEnergy);

        Damage damage = new Damage { Value = dmg, Type = DamageType.Physical };
        CmdApplyDamage(damage, enemy.gameObject);

        _energy?.SumDamageMake(dmg);
        _rune?.SumDamageMake(dmg);
    }

    [Command]
    private void CmdPlayHitSfx()
    {
        RpcPlayHitSfx();
    }

    [ClientRpc]
    private void RpcPlayHitSfx()
    {
        if (_audio && Hits != null && Hits.Length > 0)
            _audio.PlayOneShot(Hits[Random.Range(0, Hits.Length)]);
    }

    #region Talent‐setters
    public void SeriesPhysicalTalentActive(bool value) => _seriesPhysicalTalent = value;
    public void SetTalentActive(bool value) => _talentActive = value;

    public void TalentRollingPhys(bool value, float count)
    {
        _rollingPhysTalent = value;
        _stunCount = count;
    }
    #endregion

    [Command]
    private void CmdState(GameObject enemy, float time)
    {
        if (enemy.TryGetComponent(out Character ch))
            ch.CharacterState.AddState(States.Stun, time, 0, _playerLinks.gameObject, name);
    }

    private void PushBackEnemy(Character enemy)
    {
        Vector3 dir = (enemy.transform.position - _playerLinks.transform.position).normalized;
        Vector3 point = enemy.transform.position + dir;
        if (!CheckObstacleBetween(_playerLinks.transform.position, point))
            CmdPush(enemy.gameObject, point);
    }

    private bool CheckObstacleBetween(Vector3 start, Vector3 end)
    {
        Vector2 dir = (end - start).normalized;
        float dist = Vector3.Distance(start, end);

        var hits = Physics2D.BoxCastAll(start, new Vector2(1f, 1f), 0f, dir, dist, _obstacle);
        if (hits.Length > 0)
        {
            _jumpPos = hits[0].point - dir;
            return true;
        }
        return false;
    }

    [Command]
    private void CmdPush(GameObject obj, Vector2 force)
    {
        if (obj.TryGetComponent(out MoveComponent move))
            move.RpcDoMove(force, .5f);
    }

    public override void LoadTargetData(TargetInfo info) =>
        _target = info.Targets.Count > 0 ? (Character)info.Targets[0] : null;

    protected override void ClearData()
    {
        if (_target) _target.SelectedCircle.IsActive = false;
        _skillRender.SetColor(Color.green);

        if (_autoAttackJob != null) { StopCoroutine(_autoAttackJob); _autoAttackJob = null; }
        _isAttacking = false;
        _target = null;
        Hero.Move.CanMove = true;
        Hero.Move.StopLookAt();
    }
}
