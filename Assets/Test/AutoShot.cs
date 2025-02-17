using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class AutoShot : AutoAttackSkill
{
    [SerializeField] private ArrowProjectile projectile;
    [SerializeField] private HeroComponent playerLinks;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private AudioClip audioClip;

    private AudioSource _audioSource;
    private bool _isAttacking;

    protected override int AnimTriggerAutoAttack => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => true;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        StartCoroutine(AutoAttackCoroutine());
    }

    private IEnumerator AutoAttackCoroutine()
    {
        while (true)
        {
            Character target = FindNearestTarget();

            if (target != null)
            {
                AttackTarget(target);
                yield return new WaitForSeconds(AttackDelay);
            }
            else
            {
                yield return null;
            }
        }
    }

    private Character FindNearestTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, Radius, TargetsLayers);
        Character nearestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent<Character>(out Character character))
            {
                float distance = Vector3.Distance(transform.position, character.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestTarget = character;
                }
            }
        }

        return nearestTarget;
    }

    private void AttackTarget(Character target)
    {
        if (!IsTargetInRadius(Radius, target.transform)) return;

        Vector3 direction = (target.transform.position - spawnPoint.position).normalized;
        _hero.Move.LookAtTransform(target.transform);

        CmdCreateProjectile(target.transform.position);
    }

    [Command]
    private void CmdCreateProjectile(Vector3 targetPosition)
    {
        Vector3 spawnPosition = spawnPoint.position;
        Vector3 direction = (targetPosition - spawnPosition).normalized;

        ArrowProjectile newProjectile = Instantiate(projectile, spawnPosition, Quaternion.LookRotation(direction));
        newProjectile.Init(playerLinks, 0, false, this);
        NetworkServer.Spawn(newProjectile.gameObject);
        newProjectile.StartFly(direction);

        RpcPlayShotSound();
    }

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && audioClip != null)
            _audioSource.PlayOneShot(audioClip);
    }

    protected override void CastAction()
    {
        throw new System.NotImplementedException();
    }
}
