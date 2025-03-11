using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChainBlade_Scorpion : Skill
{
    [Header("BladeProjectile Settings")]
    [SerializeField] private BladeProjectile _projectilePrefab;
    [SerializeField] private HeroComponent _playerLinks;
    [SerializeField] private AudioClip _shootSound;

    private Vector3 _mousePos = Vector3.positiveInfinity;
    private AudioSource _audioSource;

    private Energy _energy;
    private bool _isBoosted;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast => true;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();

        for (int i = 0; i < _playerLinks.Resources.Count; i++)
        {
            if (_playerLinks.Resources[i].Type == ResourceType.Energy)
            {
                _energy = (Energy)_playerLinks.Resources[i];
                break;
            }
        }
    }

    #region Cast Methods

    protected override IEnumerator PrepareJob()
    {
        while (float.IsPositiveInfinity(_mousePos.x))
        {
            if (GetMouseButton)
            {
                var target = GetTarget();

                if (target.isCharater)
                {
                    float distance = Vector3.Distance(_hero.transform.position, target.character.transform.position);

                    if (distance <= Radius)
                    {
                        _mousePos = target.character.transform.position;
                    }
                    else
                    {
                        _mousePos = target.Position;
                    }
                }
                else
                {
                    _mousePos = target.Position;
                }
            }

            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        Shoot();
        Hero.Move.CanMove = true;

        yield break;
    }

    protected override void ClearData()
    {
        _mousePos = Vector3.positiveInfinity;
    }

    #endregion

    private void Shoot()
    {
        Buff.AttackSpeed.ReductionPercentage(1 + Buff.AttackSpeed.Multiplier);

        Vector3 lookDir = (_mousePos - _playerLinks.transform.position).normalized;

        Buff.AttackSpeed.IncreasePercentage(1 + Buff.AttackSpeed.Multiplier);
        CmdCreateProjectile(lookDir, _energy.CurrentValue);

        ClearData();
    }

    #region Server RPC

    [Command]
    private void CmdCreateProjectile(Vector3 direction, float energyValue)
    {
        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = Quaternion.LookRotation(direction);

        BladeProjectile projectile = Instantiate(_projectilePrefab, spawnPosition, spawnRotation);

        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);

        projectile.Init(_playerLinks, energyValue, false, this);
        projectile.StartFly(direction);

        NetworkServer.Spawn(projectile.gameObject);
        RpcInitProjectile(projectile.gameObject, direction, energyValue);
        RpcPlayShootSound();
    }

    [ClientRpc]
    private void RpcInitProjectile(GameObject obj, Vector3 direction, float energyValue)
    {
        obj.GetComponent<BladeProjectile>().Init(_playerLinks, energyValue, false, this);
        obj.transform.rotation = Quaternion.LookRotation(direction);
    }

    [ClientRpc]
    private void RpcPlayShootSound()
    {
        if (_audioSource != null && _shootSound != null)
        {
            _audioSource.PlayOneShot(_shootSound);
        }
    }

    #endregion

    #region Optional Talent Support
    public void TalentBoost(bool value)
    {
        _isBoosted = value;
    }
    #endregion
}
