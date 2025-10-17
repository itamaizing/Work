using System.Collections;
using UnityEngine;
using Mirror;

public class MucusAutoGrowth : Skill, IPassiveSkill
{
    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override IEnumerator CastJob() { yield break; }
    protected override void ClearData() { }
    protected override IEnumerator PrepareJob(System.Action<TargetInfo> targetDataSavedCallback) { yield break; }
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => false;

    [SerializeField] private Mucus mucus;

    private GameObject _mucusInstance;
    private Coroutine _growthRoutine;
    private float _initialY;
    private ObjectHealth _mucusHealth;

    private void OnEnable()
    {
        if (isServer) // Только сервер может спавнить через NetworkServer.Spawn
        {
            SpawnAndStartGrowth();
        }
    }

    private void OnDisable()
    {
        if (_growthRoutine != null)
            StopCoroutine(_growthRoutine);
    }

    [Server]
    private void SpawnAndStartGrowth()
    {
        // Спавним слизь по сети
        _mucusInstance = Instantiate(mucus.gameObject, transform.position, Quaternion.identity);
        NetworkServer.Spawn(_mucusInstance);

        _initialY = mucus.MucusObject.transform.localScale.y;
        _mucusInstance.transform.localScale = new Vector3(0f, _initialY, 0f);

        _mucusHealth = _mucusInstance.GetComponent<ObjectHealth>();

        if (_mucusHealth != null && mucus.MucusHeath != null)
        {
            _mucusHealth.InitializeObject(mucus.MucusHeath.ObjectData);
        }

        _growthRoutine = StartCoroutine(GrowMucusRoutine());
    }

    [Server]
    private IEnumerator GrowMucusRoutine()
    {
        var wait = new WaitForSeconds(1f);

        while (_mucusInstance != null)
        {
            Vector3 currentScale = _mucusInstance.transform.localScale;

            float newX = Mathf.Min(currentScale.x + 0.5f, 3f);
            float newZ = Mathf.Min(currentScale.z + 0.5f, 3f);

            _mucusInstance.transform.localScale = new Vector3(newX, _initialY, newZ);

            if (_mucusHealth != null)
            {
                mucus.MucusHeath.ObjectData.MaxHealth += 5;

                float newMax = mucus.MucusHeath.ObjectData.MaxHealth;
                _mucusHealth.MaxValue = newMax;
                _mucusHealth.CurrentHealth = newMax;

                if (_mucusHealth.TryGetComponent<ObjectBar>(out var bar))
                {
                    bar.SetMaxHealth(newMax);
                    bar.SetHealth(newMax);
                }
            }

            yield return wait;
        }
    }
}
