using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChainBlade_Scorpion : Skill
{
    [Header("Settings")]
    [SerializeField] private BladeProjectile _projectilePrefab;
    [SerializeField] private ChainEffect _chainEffectPrefab;
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
            CmdSpawnBladeProjectile(lookDir, true);

        }
        else if (_energy.CurrentValue >= _bladeManaCost && _energy.CurrentValue < _chainManaCost)
        {
            _energy.CmdUse(_bladeManaCost);
            CmdSpawnBladeProjectile(lookDir, false);
        }

        ClearData();
    }

    [Command]
    private void CmdSpawnBladeProjectile(Vector3 direction, bool isChain)
    {
        Vector3 spawnPosition = transform.position + Vector3.up * 1.5f;

        var projectile = Instantiate(_projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));
        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);

        projectile.Init(_playerLinks, _energy.CurrentValue, isChain, this);
        projectile.StartFly(direction);

        if (isChain)
        {
            var chainController = projectile.GetComponentInChildren<ChainController>();
            if (chainController != null)
            {
                chainController._startTarget = _playerLinks.transform;

                var parentNetId = _playerLinks.GetComponent<NetworkIdentity>();
                chainController.parentID = parentNetId.netId;

            }
        }

        RpcPlayShootSound();
        NetworkServer.Spawn(projectile.gameObject);
    }


    [ClientRpc]
    private void RpcPlayShootSound()
    {
        if (_audioSource != null && _shootSound != null)
            _audioSource.PlayOneShot(_shootSound);
    }
}