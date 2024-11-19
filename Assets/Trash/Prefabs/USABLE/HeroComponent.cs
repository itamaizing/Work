using System;
using UnityEngine;

public class HeroComponent : Character, IDamageable
{
    [SerializeField] private TalentSystem talentManager;
    public event Action<float, Damage, Skill> DamageTaken;

    public TalentSystem TalentManager => talentManager;

    public override void Initialize()
    {
		base.Initialize();
        TalentManager.Initialize();
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        Health.TryTakeDamage(ref damage, skill);
        DamageTaken?.Invoke(damage.Value, damage, skill);
        return true;
    }

    public void ShowPhantomValue(Damage phantomValue)
    {

    }
}