using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HidingSelectStates : NetworkBehaviour
{
    [SerializeField] private Character _player;
    private List<SpriteRenderer> _renderers = new();
    private Dictionary<SpriteRenderer, Color> _originalSpriteColors = new();
    private bool _isAlly;

    private void Awake()
    {
        _renderers.AddRange(GetComponentsInChildren<SpriteRenderer>());
        foreach (var renderer in _renderers)
        {
            _originalSpriteColors.Add(renderer, renderer.color);
        }
    }

    private void Start()
    {
        if (_player != null)
        {
            _player.OnDisappeared += OnHidingSelectCircle;
            _player.OnAppeared += OnRevealingSelectCircle;
        }
    }

    private void OnHidingSelectCircle()
    {
        PlayerTeamIndex(_player.gameObject);

        if (_isAlly)
        {
            foreach (var sprite in _renderers)
            {
                Color originalSpriteColor;

                var newSpriteTransparency = sprite.color;
                newSpriteTransparency.a = 0.33f;
                if (sprite != null && _originalSpriteColors.TryGetValue(sprite, out originalSpriteColor))
                {
                    sprite.color = new Color(originalSpriteColor.r, originalSpriteColor.g, originalSpriteColor.b, newSpriteTransparency.a);
                }
            }
        }
        else
        {
            foreach (var sprite in _renderers)
            {
                Color originalSpriteColor;

                var newSpriteTransparency = sprite.color;
                newSpriteTransparency.a = 0.0f;

                if (sprite != null && _originalSpriteColors.TryGetValue(sprite, out originalSpriteColor))
                {
                    sprite.color = new Color(originalSpriteColor.r, originalSpriteColor.g, originalSpriteColor.b, newSpriteTransparency.a);
                }
            }
        }
    }

    private void OnRevealingSelectCircle()
    {
        if (_isAlly)
        {
            foreach (var sprite in _renderers)
            {
                Color originalSpriteColor;

                if (sprite != null && _originalSpriteColors.TryGetValue(sprite, out originalSpriteColor))
                {
                    sprite.color = new Color(originalSpriteColor.r, originalSpriteColor.g, originalSpriteColor.b, originalSpriteColor.a);
                }
            }
        }
        else
        {
            foreach (var sprite in _renderers)
            {
                Color originalSpriteColor;

                if (sprite != null && _originalSpriteColors.TryGetValue(sprite, out originalSpriteColor))
                {
                    sprite.color = new Color(originalSpriteColor.r, originalSpriteColor.g, originalSpriteColor.b, originalSpriteColor.a);
                }
            }
        }
    }

    private void PlayerTeamIndex(GameObject player)
    {
        int teamIndex = player.GetComponentInParent<UserNetworkSettings>().TeamIndex;
        var localPlayer = NetworkClient.connection.identity.GetComponent<UserNetworkSettings>();
        
        if (localPlayer != null)
            _isAlly = localPlayer.TeamIndex == teamIndex;
    }
}
