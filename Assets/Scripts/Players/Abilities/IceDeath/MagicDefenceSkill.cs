using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class MagicDefenceSkill : Skill
{
    [SerializeField]private MagicDomeZone _domePrefab;
    private float _baseRuneCost = 2f;
    private float _baseDurability = 60f;
    private float _baseDuration = 2f;
    private float _energyPerStep = 10f;
    private float _durabilityPerStep = 15f;
    private float _durationPerStep = 1f;

    private float _terrRuneCost       = 4f;
    private float _terrBaseDurability = 120f;

    private int _plagueCharges;
    
    private enum CastMode { Self, Ally, Enemy, Territory }
    private CastMode _castMode;
    private Character _castTarget;
    private Vector3   _targetPoint;
    
    private const float AnimSpeedOnSelf = 0.8f;
    private const float AnimSpeedOnEnemyOfAllies = 2f;
    private const float AnimStandartSpeed = 1f;
    private const float RadiusSearchTarget = 0.5f;
    private RuneComponent _rune;

    private MagicDomeZone _tempZone;

    public MagicDomeZone TempZone => _tempZone;
    
    private Coroutine _hoverPreviewCoroutine;

    private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    
    private int MagicDefenceTrigger => Animator.StringToHash("Throw");

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render,hero);
        _rune = (RuneComponent)Hero.Resources[ResourceType.Rune];
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
        {
            _castTarget = targetInfo.GetTargets()[0] as Character;
            Targeting.SetTarget(_castTarget);
        }
        else if (targetInfo.Points.Count > 0)
        {
            _targetPoint = targetInfo.Points[0];
            _castMode    = CastMode.Territory;
            Targeting.SetTarget(new TargetData(_targetPoint));
        }
    }
    
    public void AddPlagueCharge(int value)
    {
        _plagueCharges += value;
    }
    
    private bool CanPayRunes(float cost)
    {
        int plague = _plagueCharges;
        float rune = Mathf.Max(0f, cost - plague);
        return !(_hero.TryGetResource(ResourceType.Rune, out var r)) || r.CurrentValue >= rune;
    }

    private void SpendRunes(float cost)
    {
        int plague = _plagueCharges;
        int useP   = Mathf.Min(plague, Mathf.FloorToInt(cost));
        float useR = cost - useP;

        if (useP > 0) _plagueCharges -= useP;
        if (useR > 0 && _hero.TryGetResource(ResourceType.Rune, out var r)) r.CmdUse(useR);
    }

    private (float dur, float sec) CalcShield(float baseDur, float baseSec)
    {
        float energy = _hero.TryGetResource(ResourceType.Energy, out var e) ? e.CurrentValue : 0f;
        int   steps  = Mathf.FloorToInt(energy / _energyPerStep);
        return (baseDur + steps * _durabilityPerStep, baseSec + steps * _durationPerStep);
    }
    
    private void SpendAllEnergy()
    {
        if (_hero.TryGetResource(ResourceType.Energy, out var e) && e.CurrentValue > 0f)
            e.CmdUse(e.CurrentValue);
    }
    
    protected override bool CheckResourcesOnSkill()
    {
        return _rune.CurrentValue >= _baseRuneCost;
    }
    
    public override IEnumerator CustomDrawJob(float time = 0.2f)
    {
        while (true)
        {
            bool hoveredCharacter = IsHoveringCharacter();

            if (_skillRender.TempDamageZone != null)
                _skillRender.TempDamageZone.gameObject.SetActive(!hoveredCharacter);

            yield return null;
        }
    }

    private bool IsHoveringCharacter()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, Targeting.Layer))
            return hit.transform.TryGetComponent<Character>(out _);
        return false;
    }

    public override void StopCustomDraw()
    {
        _skillRender.StopDrawArea();
    }
    
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _castTarget = null;

        while (true)
        {
            if (!GetMouseButton) { yield return null; continue; }

            Vector3 click = Targeting.GetMousePoint();
            Targeting.FindTempTarget(click, RadiusSearchTarget, canTargetSelf: true);
            var temp = Targeting.GetTempTarget();

            if (temp?.Character != null)
            {
                var ch = temp.Character;
                _castTarget = ch;
                Targeting.SetTempTarget(ch);

                if (ch == _hero)
                    _castMode = CastMode.Self;
                else if (IsAllyTarget(ch))
                    _castMode = CastMode.Ally;
                else
                    _castMode = CastMode.Enemy;
            }
            else if (click != Vector3.zero)
            {
                _castMode    = CastMode.Territory;
                _targetPoint = click;
            }
            else { yield return null; continue; }

            break;
        }

        var info = new TargetInfo();
        if (_castTarget != null) info.AddTarget(_castTarget);
        else                     info.Points.Add(_targetPoint);
        callbackDataSaved(info);
    }

    protected override IEnumerator CastJob()
    {
        switch (_castMode)
        {
            case CastMode.Self:
                yield return StartCoroutine(CastSelf());      break;
            case CastMode.Ally:
            case CastMode.Enemy:
                yield return StartCoroutine(CastSingle());    break;
            case CastMode.Territory:
                yield return StartCoroutine(CastTerritory()); break;
        }
    }

    private IEnumerator CastSelf()
    {
        PlayMagicDefenceAnim(AnimSpeedOnSelf, false);
        yield return new WaitForSeconds(0.8f);
        PlayMagicDefenceAnim(AnimStandartSpeed, true);
        if (!CanPayRunes(_baseRuneCost)) yield break;

        var (durability, duration) = CalcShield(_baseDurability, _baseDuration);
        SpendRunes(_baseRuneCost);
        SpendAllEnergy();
        CmdApplyShield(_hero.gameObject, durability, duration, false);
    }

    private IEnumerator CastSingle()
    {
        PlayMagicDefenceAnim(AnimSpeedOnEnemyOfAllies, false);
        yield return new WaitForSeconds(2f);
        PlayMagicDefenceAnim(AnimSpeedOnEnemyOfAllies, true);
        if (_castTarget == null || !CanPayRunes(_baseRuneCost)) yield break;

        var (durability, duration) = CalcShield(_baseDurability, _baseDuration);
        SpendRunes(_baseRuneCost);
        SpendAllEnergy();
        CmdApplyShield(_castTarget.gameObject, durability, duration, _castMode == CastMode.Enemy);
    }

    private IEnumerator CastTerritory()
    {
        PlayMagicDefenceAnim(AnimSpeedOnEnemyOfAllies, false);
        yield return new WaitForSeconds(2f);
        PlayMagicDefenceAnim(AnimSpeedOnEnemyOfAllies, true);
        if (!CanPayRunes(_terrRuneCost)) yield break;

        var (durability, duration) = CalcShield(_terrBaseDurability, _baseDuration);
        SpendRunes(_terrRuneCost);
        SpendAllEnergy();
        CmdSpawnDome(_targetPoint, durability, duration);
    }
    
    [Command]
    private void CmdApplyShield(GameObject targetObj, float durability, float duration, bool enemyMode)
    {
        var target = targetObj?.GetComponent<Character>();
        if (target == null) return;

        string tag = enemyMode ? $"{nameof(MagicShieldState)}_enemy" : nameof(MagicShieldState);
        target.CharacterState.AddState(States.MagicShield, duration, durability, _hero.gameObject, tag);
    }

    [Command]
    private void CmdSpawnDome(Vector3 pos, float durability, float duration)
    {
        if (_domePrefab == null) return;

        var dome = Instantiate(_domePrefab, pos, Quaternion.identity);
        dome.PreInit(15, duration);
        NetworkServer.Spawn(dome.gameObject, connectionToClient);
        dome.BeginLifetime();
        RpcInitDome(_hero.NetworkSettings.connectionToClient,dome.gameObject,duration);
        _tempZone = dome;
    }

    [TargetRpc]
    private void RpcInitDome(NetworkConnectionToClient target,GameObject domeZone,float duration)
    {
        if(!domeZone) return;
        var dome = domeZone.GetComponent<MagicDomeZone>();
        dome.ActivateAura(true,duration,true,this,_hero.gameObject);
    }

    private void PlayMagicDefenceAnim(float speed,bool isSpeedOnly)
    {
        _hero.Animator.speed = AnimStandartSpeed / speed;
        if(!isSpeedOnly)
            _hero.Animator.SetTrigger(MagicDefenceTrigger);
    }
    
    protected override void ClearData()
    {
        _castTarget = null;
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
    }
}