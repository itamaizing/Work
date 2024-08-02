using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class SpitPoison : Ability
{
    [Header("Talents")]
    [SerializeField] private HealingPoisonCloud _healingPoisonCloud;
    [SerializeField] private CapaciousPoisonCloud _capaciousPoisonCloud;
    [SerializeField] private ToxiqueCloud _toxiqueCloud;

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
    public bool ToxiqueCloudTalentIsActive => _toxiqueCloud.isActive;

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

        //CmdCreatePoisonCloudBuff(HealingCloudTalentIsActive, CapaciousCloudTalentIsActive, ToxiqueCloudTalentIsActive);
        CmdApplyCLoudPoison();
        _playerLinks.Stamina.Use(_playerLinks.Stamina.Value);

        Cancel();
    }

    #region Command Methods

    [Command]
    private void CmdInstantiateProjectile(float angle, float manaValue)
    {
        SpitPoisonProjectile projectile = Instantiate(_projectile, _playerLinks.Rb.position, Quaternion.Euler(0, 0, angle));
        projectile.InitializationProjectile(_playerLinks, manaValue);

        NetworkServer.Spawn(projectile.gameObject);

        RpcInstantiateProjectile(angle, manaValue);
        RpcInitialization(projectile.gameObject, manaValue);
    }

    [Command]
    private void CmdCreatePoisonCloudBuff(bool isActiveHealingCloud, bool isActiveCapaciousCloud, bool isActiveToxiqueCloud)
    {
        RpcCreatePoisonCloudBuff(isActiveHealingCloud, isActiveCapaciousCloud, isActiveToxiqueCloud);

        _poisonCloudBuff = _playerLinks.GetComponentInChildren<PoisonCloudBuff>();
        if (_poisonCloudBuff == null)
        {
            _poisonCloudBuff = Instantiate(_poisonCloudBuffPrefab, _playerLinks.transform);
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks, isActiveHealingCloud, isActiveCapaciousCloud, isActiveToxiqueCloud);
        }
        else
        {
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks, isActiveHealingCloud, isActiveCapaciousCloud, isActiveToxiqueCloud);
        }
    }

    #endregion

    #region ClientRpc Methods

    [ClientRpc]
    private void RpcInitialization(GameObject projectile, float manaValue)
    {
        projectile.GetComponent<SpitPoisonProjectile>().InitializationProjectile(_playerLinks, manaValue);
    }

    [ClientRpc]
    private void RpcInstantiateProjectile(float angle, float manaValue)
    {
        SpitPoisonProjectile projectile = Instantiate(_projectile, _playerLinks.Rb.position, Quaternion.Euler(0, 0, angle));
        projectile.InitializationProjectile(_playerLinks, manaValue);
    }

    [ClientRpc]
    private void RpcCreatePoisonCloudBuff(bool isActiveHealingCloud, bool isActiveCapaciousCloud, bool isActiveToxiqueCloud)
    {
        _poisonCloudBuff = _playerLinks.GetComponentInChildren<PoisonCloudBuff>();
        if (_poisonCloudBuff == null)
        {
            _poisonCloudBuff = Instantiate(_poisonCloudBuffPrefab, _playerLinks.transform);
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks, isActiveHealingCloud, isActiveCapaciousCloud, isActiveToxiqueCloud);
        }
        else
        {
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks, isActiveHealingCloud, isActiveCapaciousCloud, isActiveToxiqueCloud);
        }
    }

    #endregion
    [Command]
    private void CmdApplyCLoudPoison()
    {
        _playerLinks.CharacterState.AddState(new PoisonCloud(), 6f, 0, States.PoisonCloud);
        RpcApplyCloudPoison();
    }

    [ClientRpc]
    private void RpcApplyCloudPoison()
    {
        _playerLinks.CharacterState.AddState(new PoisonCloud(), 6f, 0, States.PoisonCloud);        
    }
}
