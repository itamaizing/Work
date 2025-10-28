using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ObjectData", menuName = "Create Object Data")]
public class ObjectData : ScriptableObject
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float regenerationAmount;
    [SerializeField] private float regenerationInterval;

    [Header("Endurance")]
    [SerializeField] private bool maxEndurance = true;
    [SerializeField] private bool minEndurance = false;

    public float MaxHealth { get => maxHealth; set => maxHealth = value; }
    public float RegenerationAmount => regenerationAmount;
    public float RegenerationInterval => regenerationInterval;

    public bool MaxEndurance => maxEndurance;
    public bool MinEndurance => minEndurance;

}
