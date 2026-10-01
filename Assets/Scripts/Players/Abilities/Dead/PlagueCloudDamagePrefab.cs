using System.Collections;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlagueCloudDamagePrefab : NetworkBehaviour
{
    [SerializeField] private LayerMask _alliesMask;
    private Character _dad;
    private const float Duration = 12f;
    private const float CheckInterval = 0.5f;
    
    private Coroutine _checkCoroutine;


    public void Init(GameObject dadObj)
    {
        if (dadObj != null)
        {
            _dad = dadObj.GetComponent<Character>();
            _checkCoroutine = StartCoroutine(CheckOverlapCoroutine());
        }
    }

    public void StartDestroying()
    {
        Invoke("DestroySelf", 3f);
    }
    
    
    private IEnumerator CheckOverlapCoroutine()
    {
        while (true)
        {
            CheckForCharactersInCloud();
            yield return new WaitForSeconds(CheckInterval);
        }
    }

    private void CheckForCharactersInCloud()
    {
        var hits = Physics.OverlapSphere(transform.position, 1f, _alliesMask);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if(hit.gameObject == _dad.gameObject) continue;
            if (!hit.TryGetComponent<Character>(out var character)) continue;
            if (character is IceDeadMinion) continue;
            if (character.CharacterState.CheckForState(States.Plague)) continue;

            _dad.Abilities.GetSkill<PortalDarkness>().CmdApplyPlague(hit.gameObject, Duration);
        }
    }
    
    private void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
}