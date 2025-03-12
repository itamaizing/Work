using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChainBlade_Scorpion : Skill
{
    [Header("Settings")]
    [SerializeField] private BladeProjectile _projectilePrefab;
    [SerializeField] private ChainController _chainControllerPrefab;
    [SerializeField] private HeroComponent _playerLinks;
    [SerializeField] private AudioClip _shootSound;

    [Header("Energy Costs")]
    [SerializeField] private float _chainManaCost = 4f;
    [SerializeField] private float _bladeManaCost = 1f;

    private Vector3 _mousePos = Vector3.positiveInfinity;
    private AudioSource _audioSource;
    private Energy _energy;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast => true;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _energy = _playerLinks.Resources.Find(r => r.Type == ResourceType.Energy) as Energy;
    }

    protected override IEnumerator PrepareJob()
    {
        while (float.IsPositiveInfinity(_mousePos.x))
        {
            if (GetMouseButton)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    _mousePos = hit.point;
                }
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        Shoot();
        Hero.Move.CanMove = false;
        yield break;
    }

    public void PullTarget(Character target)
    {
        StartCoroutine(PullRoutine(target));
    }

    private IEnumerator PullRoutine(Character target)
    {
        float speed = 6.66f;
        float minDistance = 1.5f;

        Hero.Move.CanMove = false;
        target.Move.CanMove = false;

        while (Vector3.Distance(transform.position, target.transform.position) > minDistance)
        {
            Vector3 dir = (transform.position - target.transform.position).normalized;
            target.transform.position += dir * speed * Time.deltaTime;
            yield return null;
        }

        Hero.Move.CanMove = true;
        target.Move.CanMove = true;
    }

    protected override void ClearData()
    {
        _mousePos = Vector3.positiveInfinity;
        Hero.Move.CanMove = true;
    }

    private void Shoot()
    {
        Vector3 lookDir = (_mousePos - _playerLinks.transform.position).normalized;

        if (_energy.CurrentValue >= _chainManaCost)
        {
            _energy.CmdUse(_chainManaCost);
            CmdSpawnChainProjectile(lookDir);
        }
        else if (_energy.CurrentValue >= _bladeManaCost && _energy.CurrentValue < _chainManaCost)
        {
            _energy.CmdUse(_bladeManaCost);
            CmdSpawnBladeProjectile(lookDir);
        }

        ClearData();
    }

    [Command]
    private void CmdSpawnBladeProjectile(Vector3 direction)
    {
        Vector3 spawnPosition = transform.position + Vector3.up * 1.5f;
        var projectile = Instantiate(_projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));
        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);

        projectile.Init(_playerLinks, _energy.CurrentValue, false, this);
        projectile.StartFly(direction);

        RpcPlayShootSound();
        NetworkServer.Spawn(projectile.gameObject);
    }

    [Command]
    private void CmdSpawnChainProjectile(Vector3 direction)
    {
        Vector3 spawnPosition = transform.position + Vector3.up * 1.5f;

        var projectile = Instantiate(_projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));
        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
        projectile.Init(_playerLinks, _energy.CurrentValue, false, this);
        projectile.StartFly(direction);
        NetworkServer.Spawn(projectile.gameObject);

        void OnDamageTracked(Damage dmg, GameObject targetObject)
        {
            if (targetObject.TryGetComponent<Character>(out Character targetCharacter))
            {
                var chainController = Instantiate(_chainControllerPrefab, spawnPosition, Quaternion.identity);
                SceneManager.MoveGameObjectToScene(chainController.gameObject, _hero.NetworkSettings.MyRoom);

                chainController.InitChain(_playerLinks.transform, targetCharacter.transform);
                NetworkServer.Spawn(chainController.gameObject);

                PullTarget(targetCharacter);

                _hero.DamageTracker.OnDamageTracked -= OnDamageTracked;
            }
        }

        _hero.DamageTracker.OnDamageTracked += OnDamageTracked;

        RpcPlayShootSound();
    }

    [ClientRpc]
    private void RpcPlayShootSound()
    {
        if (_audioSource != null && _shootSound != null)
            _audioSource.PlayOneShot(_shootSound);
    }
}
