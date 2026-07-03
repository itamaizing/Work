using Mirror;
using UnityEngine;

public class NewTargetFireBooster : SkillTalentHandler
{
    private const float PunchKickDamagePercent = 0.5f;
    private const float PunchKickScorchedChance = 50f;
    private const float BladeDamagePercent = 0.25f;
    private const float BladeScorchedChance = 30f;

    private readonly NewPunch_Scorpion _punch;
    private readonly Kick_Scorpion _kick;
    private readonly CleavingBlade_Scorpion _cleavingBlade;
    private readonly ChainBlade _chainBlade;

    private bool _isEnabled = false;

    public bool IsEnabled => _isEnabled;
    
    private Character _lastTarget;

    public NewTargetFireBooster(
        NetworkBehaviour owner,
        NewPunch_Scorpion punch,
        Kick_Scorpion kick,
        CleavingBlade_Scorpion cleavingBlade,
        ChainBlade chainBlade) : base(owner)
    {
        _punch = punch;
        _kick = kick;
        _cleavingBlade = cleavingBlade;
        _chainBlade = chainBlade;
    }

    public override void Enable(bool value)
    {
        _isEnabled = value;
        
        if (value)
        {
            _punch.CastStarted += OnPunchCastStarted;
            _kick.CastStarted += OnKickCastStarted;
            _cleavingBlade.CastStarted += OnBladeCastStarted;
            _chainBlade.OnArrowHit += OnChainBladeCastStarted;
        }
        else
        {
            _punch.CastStarted -= OnPunchCastStarted;
            _kick.CastStarted -= OnKickCastStarted;
            _cleavingBlade.CastStarted -= OnBladeCastStarted;
            _chainBlade.OnArrowHit -= OnChainBladeCastStarted;
        }
    }

    private void OnPunchCastStarted()
    {
        var target = _punch.Targeting.GetTarget()?.Character;
        if (target == null) return;
        if (target != _lastTarget)
        {
            _punch.AddFireBonus(PunchKickDamagePercent, PunchKickScorchedChance);
        }

        _lastTarget = target;
    }

    private void OnKickCastStarted()
    {
        var target = _kick.Targeting.GetTarget()?.Character;
        if (target == null) return;
        if (target != _lastTarget)
            _kick.AddFireBonus(PunchKickDamagePercent, PunchKickScorchedChance);
        _lastTarget = target;
    }

    private void OnBladeCastStarted()
    {
        var target = _cleavingBlade.Targeting.GetTarget()?.Character;
        if (target == null) return;
        if (target != _lastTarget)
        {
            _cleavingBlade.AddFireBonus(BladeDamagePercent, BladeScorchedChance);
        }
        _lastTarget = target;
    }
    
    private void OnChainBladeCastStarted(Character target)
    {
        if (target == null) return;
        if (target != _lastTarget)
        {
            _chainBlade.AddFireBonus(BladeDamagePercent, BladeScorchedChance);
        }
        _lastTarget = target;
    }
}