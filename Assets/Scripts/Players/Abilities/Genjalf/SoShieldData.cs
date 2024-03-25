using System.Collections.Generic;
using UnityEngine;

namespace Players.Abilities.Genjalf
{
    [CreateAssetMenu(menuName = "Create Shield Data",fileName = "Shield Data")]
    public class SoShieldData:ScriptableObject
    {
        [SerializeField] private int _shieldCharges = 3;
        [SerializeField] private float _manaCost = 20f;
        [SerializeField] private float _absorptionAmount = 30f;
        [SerializeField] private float _durationShield = 2f;

        public int shieldCharges => _shieldCharges;

        public float manaCost => _manaCost;

        public float AbsorptionAmount => _absorptionAmount;

        public float DurationShield => _durationShield;
    }
}