using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class DamageTracker : NetworkBehaviour
{
    private List<DamageEntry> _localDamageEntries = new List<DamageEntry>();
    private List<HealEntry> _localHealEntries = new List<HealEntry>();

    public SyncList<DamageEntry> _damageEntries = new SyncList<DamageEntry>();
    public SyncList<HealEntry> _healEntries = new SyncList<HealEntry>();

    public List<DamageEntry> GetLocalDamageEntries => _localDamageEntries;
    public List<HealEntry> GetLocalHealEntries => _localHealEntries;
    
    public void AddDamage(Damage damage, bool isServerRequest = false)
    {
        if (!isServerRequest)
        {
            CmdAddDamage(damage);
        }
        
        _damageEntries.Add(new DamageEntry(damage, Time.time));
        Debug.Log($"[DamageTracker] Damage added: {damage.Value}, Time: {Time.time}, School: {damage.School}");
    }
    
    [Command]
    private void CmdAddDamage(Damage damage)
    {
        AddDamage(damage, true);
    }

    public void AddHeal(Heal heal, bool isServerRequest = false)
    {
        if (!isServerRequest)
        {
            CmdAddHeal(heal);
        }
        
        _healEntries.Add(new HealEntry(heal, Time.time));
        Debug.Log($"[DamageTracker] Heal added: {heal.Value}, Time: {Time.time}");
    }
    
    [Command]
    private void CmdAddHeal(Heal heal)
    {
        AddHeal(heal, true);
    }
    
    public float GetLocalDamageInTime(Schools school, float time)
    {
        RemoveOldLocalEntries();
        return _damageEntries.Where(o => o.Damage.School == school)
            .Where(o => o.Time >= Time.time - time)
            .Sum(o => o.Damage.Value);
    }
    
    public float GetLocalHealInTime(float time)
    {
        RemoveOldLocalEntries();
        return _healEntries
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