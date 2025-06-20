using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargeCDUI : MonoBehaviour
{
    [SerializeField] private FillAmountOverTime _chargeCDPref;

    private List<FillAmountOverTime> _chargeCDList = new();

    public void AddChargeCD(float cooldown)
    {
        var charge = Instantiate(_chargeCDPref, transform);
        _chargeCDList.Add(charge);

        charge.Ended += OnEnded;

        charge.StartFill(cooldown);
    }

    private void OnEnded(FillAmountOverTime charge)
    {
        charge.Ended -= OnEnded;
        Destroy(charge.gameObject);
    }
}
