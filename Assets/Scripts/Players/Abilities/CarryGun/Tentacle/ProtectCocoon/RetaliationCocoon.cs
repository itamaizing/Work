using Mirror;
using UnityEngine;

public class RetaliationCocoon : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private ObjectHealth _objectHealth;
    [SerializeField] private DamageType damageType = DamageType.Physical;
    [SerializeField] private float maxDistance = 4f;
    [SerializeField] private LightningAttack _lightningPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private ProtectiveCocoon _protectiveCocoon;

    private void OnEnable()
    {
        if (_objectHealth != null)
            _objectHealth.DamageTaken += OnObjectDamaged;
    }

    private void OnDisable()
    {
        if (_objectHealth != null)
            _objectHealth.DamageTaken -= OnObjectDamaged;
    }

    private void OnObjectDamaged(Damage damage, Skill skill)
    {
        if (!isServer) return;

        if (damage.Type != DamageType.Physical) return;

        if (skill == null) return;
        if (skill.Hero == null) return;

        Character attacker = skill.Hero;
        if (attacker == null) return;

        if (!IsInRange(attacker.transform.position))
            return;

        ApplyDamage(_protectiveCocoon.BaseDamage, damageType, attacker.gameObject);
        RpcSpawnLightning(attacker.netIdentity);
    }

    private bool IsInRange(Vector3 attackerPosition)
    {
        Vector3 cocoonPos = _spawnPoint != null
            ? _spawnPoint.position
            : transform.position;

        float sqrDist = (attackerPosition - cocoonPos).sqrMagnitude;

        return sqrDist <= maxDistance * maxDistance;
    }

    private void ApplyDamage(float value, DamageType type, GameObject target)
    {
        if (_protectiveCocoon == null) return;
        if (_protectiveCocoon.SkillHero == null) return;

        Damage retaliation = new Damage
        {
            Value = value,
            Type = type,
            School = _protectiveCocoon.SkillHero.Info.School
        };

        _protectiveCocoon.SkillHero.ApplyDamage(retaliation, target);
    }

    [ClientRpc]
    private void RpcSpawnLightning(NetworkIdentity targetIdentity)
    {
        if (targetIdentity == null) return;
        if (_lightningPrefab == null) return;

        LightningAttack lightning = Instantiate(_lightningPrefab);
        lightning.Init(_spawnPoint.position, targetIdentity.transform);
    }
}