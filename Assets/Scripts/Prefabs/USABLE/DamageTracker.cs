using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class DamageTracker : NetworkBehaviour
{
    private List<DamageEntry> _localDamageEntries = new List<DamageEntry>();
    private List<HealEntry> _localHealEntries = new List<HealEntry>();

    private readonly SyncList<DamageEntry> _damageEntries = new SyncList<DamageEntry>();
    private readonly SyncList<HealEntry> _healEntries = new SyncList<HealEntry>();

    public List<DamageEntry> GetLocalDamageEntries => _localDamageEntries;
    public List<HealEntry> GetLocalHealEntries => _localHealEntries;
    
    public void AddDamage(Damage damage)
    {
        if (isOwned)
        {
            _localDamageEntries.Add(new DamageEntry(damage, Time.time));
            Debug.Log($"[DamageTracker] Local Damage added: {damage.Value}, Time: {Time.time}, School: {damage.School}");
            
            CmdAddDamage(damage);
        }
    }
    
    [Command]
    private void CmdAddDamage(Damage damage)
    {
        _damageEntries.Add(new DamageEntry(damage, Time.time));
        Debug.Log($"[DamageTracker] Damage added on server: {damage.Value}, Time: {Time.time}, School: {damage.School}");
    }
    
    public void AddHeal(Heal heal)
    {
        if (isOwned)
        {
            _localHealEntries.Add(new HealEntry(heal, Time.time));
            Debug.Log($"[DamageTracker] Local Heal added: {heal.Value}, Time: {Time.time}");

            CmdAddHeal(heal);
        }
    }
    
    [Command]
    private void CmdAddHeal(Heal heal)
    {
        _healEntries.Add(new HealEntry(heal, Time.time));
        Debug.Log($"[DamageTracker] Heal added on server: {heal.Value}, Time: {Time.time}");
    }
    
    public float GetLocalDamageInTime(Schools school, float time)
    {
        RemoveOldLocalEntries();
        return _localDamageEntries.Where(o => o.Damage.School == school)
            .Where(o => o.Time >= Time.time - time)
            .Sum(o => o.Damage.Value);
    }
    
    public float GetLocalHealInTime(float time)
    {
        RemoveOldLocalEntries();
        return _localHealEntries
            .Where(o => o.Time >= Time.time - time)
            .Sum(o => o.Heal.Value);
    }
    
    public void RemoveOldLocalEntries(float timeLimit = 10f)
    {
        _localDamageEntries.RemoveAll(o => Time.time - o.Time > timeLimit);
        _localHealEntries.RemoveAll(o => Time.time - o.Time > timeLimit);
    }
}

public struct DamageEntry
{
    public Damage Damage;
    public float Time;

    public DamageEntry(Damage damage, float time)
    {
        Damage = damage;
        Time = time;
    }
}

public struct HealEntry
{
    public Heal Heal;
    public float Time;

    public HealEntry(Heal damage, float time)
    {
        Heal = damage;
        Time = time;
    }
}