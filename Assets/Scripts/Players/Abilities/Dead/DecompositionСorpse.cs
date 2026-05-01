using Mirror;
using UnityEngine;
using System.Collections;

public class DecompositionCorpse : NetworkBehaviour
{
    [SerializeField] private GameObject _plagueCloudPrefab;

    private Health _health;
    private Coroutine _decompositionRoutine;

    private const float DamagePerTick = 1f;
    private const float Interval = 1f;

    private void OnEnable()
    {
        _health = GetComponent<Health>();

        if (_health == null) return;

        _health.Died += OnDied;

        _decompositionRoutine = StartCoroutine(DecompositionRoutine());
    }

    private IEnumerator DecompositionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Interval);

            if (_health == null) yield break;

            bool alive = _health.TryUse(DamagePerTick);

            if (!alive) yield break;
        }
    }

    private void OnDied()
    {
        if (_decompositionRoutine != null)
        {
            StopCoroutine(_decompositionRoutine);
            _decompositionRoutine = null;
        }

        if (_health != null) _health.Died -= OnDied;

        SpawnCloud(transform.position);
    }

    private void OnDestroy()
    {
        if (_health != null) _health.Died -= OnDied;
    }

    [Command]
    private void SpawnCloud(Vector3 position)
    {
        GameObject cloud = Instantiate(_plagueCloudPrefab, position, Quaternion.identity);

        NetworkServer.Spawn(cloud);
    }
}