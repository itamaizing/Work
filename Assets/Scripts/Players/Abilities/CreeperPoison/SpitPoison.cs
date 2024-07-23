using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class SpitPoison : Ability
{
    [Header("Talents")]
    [SerializeField] private TalentSystem _talentSystem;
    [SerializeField] private HealingPoisonCloud _healingPoisonCloud;
    [SerializeField] private CapaciousPoisonCloud _capaciousPoisonCloud;

    [SerializeField] private PoisonCloudBuff _poisonCloudBuffPrefab;
    [SerializeField] private SpitPoisonProjectile _projectile;
    [SerializeField] private Character _playerLinks;

    private float _angle;

    private PoisonCloudBuff _poisonCloudBuff;

    private Vector2 _mousePos;

    private Coroutine _useCoroutine;
    private Coroutine _shootCoroutine;
    private Coroutine _mouseDirectionCoroutine;

    public bool HealingCloudTalentIsActive => _healingPoisonCloud.isActive;
    public bool CapaciousCloudTalentIsActive => _capaciousPoisonCloud.isActive;

    protected override void Cancel()
    {

        if (_useCoroutine != null)
            StopCoroutine(UseCoroutine());

        if (_shootCoroutine != null)
            StopCoroutine(CallShootCoroutine());

        if (_mouseDirectionCoroutine != null)
            StopCoroutine(MouseDirectionCoroutine());
    }

    protected override void Cast()
    {
        _useCoroutine = StartCoroutine(UseCoroutine()); 
    }

    private IEnumerator UseCoroutine()
    {
        yield return _mouseDirectionCoroutine = StartCoroutine(MouseDirectionCoroutine());
        _shootCoroutine = StartCoroutine(CallShootCoroutine());
    }

    private IEnumerator MouseDirectionCoroutine()
    {
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }

        _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
        _angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
    }

    private IEnumerator CallShootCoroutine()
    {
        PayCost();
        Shoot();
        yield return null;
    }

    
    private void Shoot()
    {
        CmdInstantiateProjectile(_angle, _playerLinks.Stamina.Value);

        CmdCreatePoisonCloudBuff(HealingCloudTalentIsActive, CapaciousCloudTalentIsActive);

        _playerLinks.Stamina.Use(_playerLinks.Stamina.Value);

        Cancel();
    }

    #region Command Methods

    [Command]
    private void CmdCreatePoisonCloudBuff(bool isActiveHealingCloud, bool isActiveCapaciousCloud)
    {
        RpcCreatePoisonCloudBuff(isActiveHealingCloud, isActiveCapaciousCloud);

        _poisonCloudBuff = _playerLinks.GetComponentInChildren<PoisonCloudBuff>();
        if (_poisonCloudBuff == null)
        {
            _poisonCloudBuff = Instantiate(_poisonCloudBuffPrefab, _playerLinks.transform);
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks, isActiveHealingCloud, isActiveCapaciousCloud);
        }
        else
        {
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks, isActiveHealingCloud, isActiveCapaciousCloud);
        }
    }

    [Command]
    private void CmdInstantiateProjectile(float angle, float manaValue)
    {
        SpitPoisonProjectile projectile = Instantiate(_projectile, _playerLinks.Rb.position, Quaternion.Euler(0, 0, angle));
        projectile.InitializationProjectile(_playerLinks, manaValue);

        NetworkServer.Spawn(projectile.gameObject);

        RpcInitialization(projectile.gameObject, manaValue);
    }

    #endregion

    #region ClientRpc Methods

    [ClientRpc]
    private void RpcInitialization(GameObject projectile, float manaValue)
    {
        projectile.GetComponent<SpitPoisonProjectile>().InitializationProjectile(_playerLinks, manaValue);
    }

    [ClientRpc]
    private void RpcCreatePoisonCloudBuff(bool isActiveHealingCloud, bool isActiveCapaciousCloud)
    {
        _poisonCloudBuff = _playerLinks.GetComponentInChildren<PoisonCloudBuff>();
        if (_poisonCloudBuff == null)
        {
            _poisonCloudBuff = Instantiate(_poisonCloudBuffPrefab, _playerLinks.transform);
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks, isActiveHealingCloud, isActiveCapaciousCloud);
        }
        else
        {
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks, isActiveHealingCloud, isActiveCapaciousCloud);
        }
    }

    #endregion
}
