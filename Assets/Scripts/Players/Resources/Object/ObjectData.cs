using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ObjectData", menuName = "Create Object Data")]
public class ObjectData : ScriptableObject
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float regenerationRate;

    [Header("Endurance")]
    [SerializeField] private bool endurance = true;

    public float MaxHealth => maxHealth;
    public float RegenerationRate => regenerationRate;

    public bool Endurance => endurance;
}
