using Mirror;
using UnityEngine;
using System.Collections;

public class DecompositionCorpse : NetworkBehaviour
{
    [SerializeField] private GameObject _plagueCloudPrefab;

    private Health _health;
    private Coroutine _decompositionRoutine;

    private const float DamagePerTick = 5f;
    private const float Interval = 1f;

    private void OnEnable()
    {
        _health = GetComponent<Health>();

        if (_health == null) return;

        _health.Died += OnDied;
        if(!_health.isServer)
            _decompositionRoutine = StartCoroutine(DecompositionRoutine());
    }

    private IEnumerator DecompositionRoutine()
    {
        if (isServer) yield break;
        while (true)
        {
            yield return new WaitForSeconds(Interval);

            if (_health == null) yield break;

            var dmg = new Damage { Value = DamagePerTick, Form = AbilityForm.Physical };
            if (_health.isClient)
            {
                _health.CmdTryTakeDamage(dmg, null);
            }


            bool alive = _health.CurrentValue >= 0;
            
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
    
    private void SpawnCloud(Vector3 position)
    {
        GameObject cloud = Instantiate(_plagueCloudPrefab, position, Quaternion.identity);

        NetworkServer.Spawn(cloud);
    }
}