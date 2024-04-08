using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Telekinesis : Ability
{
    [Header("Ability settings")]
    [SerializeField] private float _duration;
    [SerializeField] private float _manaCostRate;
    [SerializeField] private float _manaCost;
    [SerializeField] private float _speed;
    [SerializeField] private float _radius;
    [SerializeField] private float _range;
    [SerializeField] private DrawCircle _drawCircle;

    public override void Cancel()
    {
        
    }

    public override void Use()
    {
        
    }
}
