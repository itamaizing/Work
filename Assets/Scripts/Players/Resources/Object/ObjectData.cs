using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ObjectData", menuName = "Create Object Data")]
public class ObjectData : ScriptableObject
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float regenerationRate;
    [SerializeField] private float regenerationTime;

    [Header("Endurance")]
    [SerializeField] private bool maxEndurance = true;
    [SerializeField] private bool minEndurance = false;

    public float MaxHealth { get => maxHealth; set => maxHealth = value; }
    public float RegenerationRate => regenerationRate;
    public float RegenerationTime => regenerationTime;

    public bool MaxEndurance => maxEndurance;
    public bool MinEndurance => minEndurance;

}
