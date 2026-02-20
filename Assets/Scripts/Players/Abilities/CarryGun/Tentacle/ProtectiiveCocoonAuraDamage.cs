using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProtectiiveCocoonAuraDamage : NetworkBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageValue = 2f;
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private DamageType damageType = DamageType.Physical;

    [Header("References")]
    [SerializeField] private LayerMask characterLayer;
    [SerializeField] private LightningProjectile _lightningPrefab;
    [SerializeField] private ProtectiveCocoon _protectiveCocoon;
    [SerializeField] private Transform _spawnPoint;

    private readonly Dictionary<Character, Coroutine> _enemyCoroutines = new();
    private WaitForSeconds _wait;

    private void Awake()
    {
        _wait = new WaitForSeconds(tickInterval);

        if (_protectiveCocoon == null)
            _protectiveCocoon = GetComponentInParent<ProtectiveCocoon>();
    }

    public override void OnStopServer()
    {
        foreach (var pair in _enemyCoroutines)
        {
            if (pair.Value != null)
                StopCoroutine(pair.Value);
        }

        _enemyCoroutines.Clear();
    }

    [Server]
    public void HandleTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out Character character))
            return;

        if (_enemyCoroutines.ContainsKey(character))
            return;

        if (!IsEnemy(character, other.gameObject))
            return;

        Coroutine routine = StartCoroutine(ShootRoutine(character));
        _enemyCoroutines.Add(character, routine);
    }

    [Server]
    public void HandleTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out Character character))
            return;

        if (_enemyCoroutines.TryGetValue(character, out Coroutine routine))
        {
            StopCoroutine(routine);
            _enemyCoroutines.Remove(character);
        }
    }

    [Server]
    private IEnumerator ShootRoutine(Character target)
    {
        yield return new WaitForSeconds(Random.Range(0f, tickInterval));

        while (target != null)
        {
            SpawnProjectile(target);
            yield return _wait;
        }

        _enemyCoroutines.Remove(target);
    }

    [Server]
    private void SpawnProjectile(Character target)
    {
        if (_protectiveCocoon == null) return;
        if (_protectiveCocoon.Hero == null) return;
        if (_protectiveCocoon.SkillHero == null) return;
        if (_lightningPrefab == null) return;
        if (_spawnPoint == null) return;

        LightningProjectile projectile = Instantiate( _lightningPrefab, _spawnPoint.position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(projectile.gameObject, _protectiveCocoon.Hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(projectile.gameObject);
        projectile.Init(_protectiveCocoon.Hero, 0f, false, _protectiveCocoon.SkillHero, target, damageValue, damageType);
        projectile.StartFly(target.transform);
    }

    private bool IsEnemy(Character characterTarget, GameObject target)
    {
        if (_protectiveCocoon == null || _protectiveCocoon.Hero == null)
            return IsEnemyByLayer(target);

        if (!_protectiveCocoon.Hero.TryGetComponent(out UserNetworkSettings ownerSettings) ||
            !characterTarget.TryGetComponent(out UserNetworkSettings targetSettings))
            return IsEnemyByLayer(target);

        if (ownerSettings.TeamIndex == 0 || targetSettings.TeamIndex == 0)
            return IsEnemyByLayer(target);

        return ownerSettings.TeamIndex != targetSettings.TeamIndex;
    }

    private bool IsEnemyByLayer(GameObject target)
    {
        return ((1 << target.layer) & characterLayer.value) != 0;
    }
}
