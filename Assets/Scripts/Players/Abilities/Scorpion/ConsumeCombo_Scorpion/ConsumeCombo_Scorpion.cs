using JetBrains.Annotations;
using Mirror;
using Org.BouncyCastle.Asn1.Cmp;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConsumeCombo_Scorpion : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private ComboPoints_Player _comboPlayer;
    [SerializeField] private List<ICanConsumeComboPoints> _abilitiesToNotify = new List<ICanConsumeComboPoints>();
    private int _availablePoints;
    public int AvailablePoints { get { return RecalculateFreePoints(); } }

    public int Count = 0;
    [field: SerializeField] public bool IsActive { get; private set; }

    protected override bool IsCanCast { get { return true; } }

    protected override void Awake()
    { 
        base.Awake();

        var newList = GetComponent<SkillManager>().Abilities/*.Where(x => x is ICanConsumeComboPoints).ToList()*/;

        foreach (var item in newList)
        {
            if (item is ICanConsumeComboPoints)
            {
                _abilitiesToNotify.Add(item as ICanConsumeComboPoints);
            }
        }

        foreach (var item in _abilitiesToNotify)
        {
            item.Notifier = this;
        }
    }

    [Command]
    private void cmdtest()
    {
        Debug.LogWarning($" Cast speed test before !!!!!: {Buff.CastSpeed.Multiplier}");
        Buff.CastSpeed.IncreasePercentage(2f);
        Debug.LogWarning($" Cast speed test after !!!!!: {Buff.CastSpeed.Multiplier}");

        rpctest();
    }
    [ClientRpc]
    private void rpctest()
    {
        Debug.LogWarning($" Cast speed test before !!!!!: {Buff.CastSpeed.Multiplier}");
        Buff.CastSpeed.ReductionPercentage(0.3f);
        Debug.LogWarning($" Cast speed test after !!!!!: {Buff.CastSpeed.Multiplier}");
    }

    private void NotifyAbilities(bool State)
    {
        RecalculateFreePoints();

        //foreach (var item in _abilitiesToNotify)
        //{
        //    item.IsUsingCombo = State;
        //}
    }
    private void ResetValues()
    {
        Count = 0;
        IsActive = false;
    }

    private bool Consume()
    {
        if (_playerLinks == null)
            return false;

        if (_comboPlayer.CurrentValue == 0)
        {
            ResetValues();
            return false;
        }

        if (_comboPlayer.CurrentValue > Count)
        {
            Count++;
            IsActive = true;
        }
        else
        {
            Count = 1;
            IsActive = true;
        }


        //if (_playerLinks.Combo_Player.Use(1))
        //{
        //    return true;
        //}

        return false;
    }

    private int RecalculateFreePoints()
    {
        return (int) _comboPlayer.CurrentValue;
    }

    public int PayComboPoints(int amount)
    {
        int usedPoints = Mathf.Clamp(amount, 0, (int)_comboPlayer.CurrentValue);
        Debug.LogWarning($"Used Combo: {usedPoints}");
        if (usedPoints <= 0)
            return 0;
        //CmdUse(usedPoints);
        _comboPlayer.CmdUse(usedPoints);
        //_comboPlayer.Use(1);
        return usedPoints;
    }
    //[Command]
    //private void CmdUse(int amount)
    //{
    //    _comboPlayer.Use(amount);
    //}

    protected override IEnumerator PrepareJob()
    {
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        IsActive = !IsActive;
        yield return null;
    }

    protected override void ClearData()
    {
        
    }
}

public interface ICanConsumeComboPoints
{
    public ConsumeCombo_Scorpion Notifier { get; set; }
    public int ConsumedAmount { get; set; }
    public void TryUpgradeByConsumingCombo(int amount);
}