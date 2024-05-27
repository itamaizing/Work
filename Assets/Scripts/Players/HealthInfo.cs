using UnityEngine;

[CreateAssetMenu(menuName = "HealthInfo", fileName = "HealthInfo")]
public class HealthInfo : ScriptableObject
{
    [SerializeField] private float _defPhysDamage;
    [SerializeField] private float _defMagDamage;

    [SerializeField] private float _evadeMeleeDamage;
    [SerializeField] private float _evadeRangeDamage;
    [SerializeField] private float _evadeMagDamage;

    [SerializeField] private float _absorbPhysDamage;
    [SerializeField] private float _absorbMagDamage;

    public float DefaultPhysicsDamage => _defPhysDamage;
    public float DefaultMagicDamage => _defMagDamage;
    public float EvadeMeleeDamage => _evadeMeleeDamage;
    public float EvadeRangeDamage => _evadeRangeDamage;
    public float EvadeMagicDamage => _evadeMagDamage;
    public float AbsorbPhysicsDamage => _absorbPhysDamage;
    public float AbsorbMagicDamage => _absorbMagDamage;
}
