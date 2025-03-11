using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChainBlade_Scorpion : Skill
{
    [Header("Settings")]
    [SerializeField] private BladeProjectile _projectilePrefab;
    [SerializeField] private ChainController _chainController;
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

        _chainController.gameObject.SetActive(false);
    }

    protected override IEnumerator PrepareJob()
    {
        while (float.IsPositiveInfinity(_mousePos.x))
        {
            if (GetMouseButton)
            {
                _mousePos = GetTarget().Position;
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
            CmdActivateChainController(lookDir);
        }
        else if (_energy.CurrentValue >= _bladeManaCost)
        {
            _energy.CmdUse(_bladeManaCost);
            CmdSpawnBladeProjectile(lookDir);
        }

        ClearData();
    }

    [Command]
    private void CmdSpawnBladeProjectile(Vector3 direction)
    {
        var projectile = Instantiate(_projectilePrefab, transform.position, Quaternion.LookRotation(direction));
        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);

        projectile.Init(_playerLinks, _energy.CurrentValue, false, this);
        projectile.StartFly(direction);

        RpcPlayShootSound();
        NetworkServer.Spawn(projectile.gameObject);
    }

    [Command]
    private void CmdActivateChainController(Vector3 direction)
    {
        RpcActivateChainController(direction);
        RpcPlayShootSound();
    }

    [ClientRpc]
    private void RpcActivateChainController(Vector3 direction)
    {
        _chainController.gameObject.SetActive(true);
        _chainController.transform.position = transform.position;
        _chainController.Init(transform, direction, this);
    }

    [ClientRpc]
    private void RpcPlayShootSound()
    {
        if (_audioSource != null && _shootSound != null)
            _audioSource.PlayOneShot(_shootSound);
    }

    public void PullTarget(Character target)
    {
        StartCoroutine(PullRoutine(target));
    }

    private IEnumerator PullRoutine(Character target)
    {
        float speed = 6.66f;
        float minDistance = 1.5f;

        while (Vector3.Distance(transform.position, target.transform.position) > minDistance)
        {
            Vector3 dir = (transform.position - target.transform.position).normalized;
            target.transform.position += dir * speed * Time.deltaTime;

            _chainController.UpdatePositions();
            yield return null;
        }

        _chainController.gameObject.SetActive(false);
    }
}
