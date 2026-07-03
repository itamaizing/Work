using System;
using UnityEngine;

public interface IComboSeriesParticipatingSkill
{    
    public delegate void OnBeforeApplyDamageDelegate(ref Damage damage, Skill skill,GameObject target);
    
    public event OnBeforeApplyDamageDelegate OnBeforeApplySeriesDamage;
    
    public event Action<GameObject, Skill> OnSeriesDamaged;

    float EnergyCostOnHit { get; }
    float RuneCostOnHit { get; }
    
    bool IsTicking { get; }
    bool IgnoresEnergyCostCheck => false;

    void OnSeriesHit(int hitCountInCurrentSeries, Character target);
    void OnSeriesCompleted(Character target, int totalHits, float totalEnergySpent);
    void OnSeriesBroken(Character target);
    
    void OnSeriesPotentialFinal(Skill skill, bool isPotentialFinal);
}
