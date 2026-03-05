using System;
using System.Collections;
using System.Collections.Generic;
using Gangdollarff;
using Mirror;
using UnityEngine;

public class QuicksandTile : FisuraTile
{
    [SerializeField] private ParticleSystem _sandParticle;

    [SyncVar] private byte _ownerTeamIndex;

    private AttributeModifier _modifier = new(-0.8f, ModifierType.Percent);

    private List<GameObject> _charTemp = new();
    private bool _isInvisible;
    
    private void Start()
    {
        base.Start();
        _collider.isTrigger = true;
    }

    public void SetOwnerTeam(byte teamIndex, bool isInvisible, int bonusLength = 0, float bonusWidth = 0f)
    {
        _ownerTeamIndex = teamIndex;
        _isInvisible = isInvisible;
    
        if (bonusLength != 0) AddMaxSize(bonusLength);
        if (bonusWidth != 0f) AddWidth(bonusWidth);
    }

    public override void Build()
    {
        base.Build();

        var shape = _sandParticle.shape;
        shape.scale = new Vector3(_collider.size.x, _collider.size.z);
        _sandParticle.gameObject.transform.localPosition = _collider.center;
        
        if(_isInvisible)
            StartCoroutine(HideForEnemiesAfterDelay(1f));
    }

    private IEnumerator HideForEnemiesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RpcHideForEnemies(_ownerTeamIndex);
    }

    [ClientRpc]
    private void RpcHideForEnemies(byte ownerTeamIndex)
    {
        var localPlayers = FindObjectsOfType<UserNetworkSettings>();
        byte localTeamIndex = 0;

        foreach (var player in localPlayers)
        {
            if (player.isOwned)
            {
                localTeamIndex = player.TeamIndex;
                break;
            }
        }

        bool isEnemy = localTeamIndex != ownerTeamIndex;
        SetRenderersVisible(!isEnemy);
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (var t in _tiles)
        {
            if (t != null && visible != true) t.SetActive(false);
        }

        if (_sandParticle != null && !visible)
        {
            _sandParticle.Stop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.LogError("On entered");
        if (other.TryGetComponent(out Character character))
        {
            Debug.LogError("CharacterFounded");
            if (character.NetworkSettings.TeamIndex != _ownerTeamIndex)
            {
                Debug.LogError("Not owner");
                _charTemp.Add(other.gameObject);
                ChangeMoveSpeed(character.gameObject);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Character character))
        {
            if (character.NetworkSettings.TeamIndex != _ownerTeamIndex)
            {
                SetDefaultSpeed(character.gameObject);
                _charTemp.Remove(other.gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var character in _charTemp)
            SetDefaultSpeed(character);
    
        _charTemp.Clear();
    }

    private void ChangeMoveSpeed(GameObject target)
    {
       if (target.TryGetComponent(out Character character))
           character.Move.AddModifier(_modifier);
    }

    private void SetDefaultSpeed(GameObject target)
    {
        if (target.TryGetComponent(out Character character))
            character.Move.RemoveModifier(_modifier);
    }
}
