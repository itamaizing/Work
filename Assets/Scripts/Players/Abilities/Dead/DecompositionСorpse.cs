using Mirror;
using UnityEngine;
using System.Collections;

public class DecompositionCorpse : Skill, IPassiveSkill
{
    [SerializeField] private PlagueCloudDamagePrefab _plagueCloudPrefab;

    private Coroutine _decompositionRoutine;

    private const float DamagePerTick = 5f;
    private const float Interval = 1f;

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        if (_hero.Health == null) return;

        _hero.Health.Died += OnDied;
        if (!_hero.isServer)
            _decompositionRoutine = StartCoroutine(DecompositionRoutine());
    }

    private IEnumerator DecompositionRoutine()
    {
        if (isServer) yield break;
        while (true)
        {
            yield return new WaitForSeconds(Interval);

            if (_hero.Health == null) yield break;

            var dmg = new Damage { Value = DamagePerTick, Form = AbilityForm.Physical };
            if (_hero.Health.isClient)
            {
                _hero.Health.CmdTryTakeDamage(dmg, null);
            }


            bool alive = _hero.Health.CurrentValue >= 0;

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

        if (_hero.Health != null) _hero.Health.Died -= OnDied;

        SpawnCloud(transform.position);
    }

    private void SpawnCloud(Vector3 position)
    {
        PlagueCloudDamagePrefab cloud = Instantiate(_plagueCloudPrefab, position, Quaternion.identity);

        NetworkServer.Spawn(cloud.gameObject);
        RpcInitCloud(cloud.gameObject);
        cloud.StartDestroying();
    }

    [ClientRpc]
    private void RpcInitCloud(GameObject cloudObj)
    {
        if(cloudObj == null) return;
        var cloud = cloudObj.GetComponent<PlagueCloudDamagePrefab>();
        cloud.Init((_hero as MinionComponent)?.CharacterParent.gameObject);
    }

    protected override IEnumerator CastJob()
    {
        yield break;
    }

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
}