using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HidingSelectStates : NetworkBehaviour
{
    [SerializeField] private Character _player;
    private List<SpriteRenderer> _renderers = new();

    private bool _isAlly;

    private void Awake()
    {
        _renderers.AddRange(GetComponentsInChildren<SpriteRenderer>());

        Debug.Log("HidingSelectStates / renderers.count = " + _renderers.Count);
    }

    private void Start()
    {
        if (_player != null)
        {
            _player.OnHidingUIElements += OnHidingSelectCircle;
            _player.OnRevealingUIElements += OnRevealingSelectCircle;
        }
    }

    private void OnHidingSelectCircle()
    {
        PlayerTeamIndex(_player.gameObject);

        if (_isAlly)
        {
            foreach (var sprite in _renderers)
            {
                var newSpriteTransparency = sprite.color;
                newSpriteTransparency.a = 0.4f;
                sprite.color = new Color(0.035f, 1.0f, 0f, newSpriteTransparency.a);
            }
        }
        else
        {
            foreach (var sprite in _renderers)
            {
                var newSpriteTransparency = sprite.color;
                newSpriteTransparency.a = 0.0f;
                sprite.color = new Color(0.035f, 1.0f, 0f, newSpriteTransparency.a);
            }
        }
    }

    private void OnRevealingSelectCircle()
    {
        if (_isAlly)
        {
            foreach (var sprite in _renderers)
            {
                var newSpriteTransparency = sprite.color;
                newSpriteTransparency.a = 0.6667f;
                sprite.color = new Color(0.035f, 1.0f, 0f, newSpriteTransparency.a);
            }
        }
        else
        {
            foreach (var sprite in _renderers)
            {
                var newSpriteTransparency = sprite.color;
                newSpriteTransparency.a = 0.6667f;
                sprite.color = new Color(0.035f, 1.0f, 0f, newSpriteTransparency.a);
            }
        }
    }

    private void PlayerTeamIndex(GameObject player)
    {
        int teamIndex = player.GetComponentInParent<UserNetworkSettings>().TeamIndex;
        var localPlayer = NetworkClient.connection.identity.GetComponent<UserNetworkSettings>();
        _isAlly = localPlayer.TeamIndex == teamIndex;
    }
}
