using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class CreeperPoisonAura : NetworkBehaviour
{
    private bool _isFeelingPoisoning = false;

    public bool IsFeelingPoisoning { get => _isFeelingPoisoning; set => _isFeelingPoisoning = value; }

    public void FeelingPoisoning(bool value) => _isFeelingPoisoning = value;
}
