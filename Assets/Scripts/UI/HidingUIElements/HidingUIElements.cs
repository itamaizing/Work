using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HidingUIElements : NetworkBehaviour
{
    [SerializeField] private Character _player;
    private List<Image> _images = new();
    private List<TMP_Text> _texts = new();
    
    private bool _isAlly;

    private void Awake()
    {
        _images.AddRange(GetComponentsInChildren<Image>());

        _texts.AddRange(GetComponentsInChildren<TMP_Text>());

        Debug.Log("HidingUIElements / images.count = " + _images.Count);
        Debug.Log("HidingUIElements / texts.count = " + _texts.Count);
    }

    private void Start()
    {
        if (_player != null)
        {
            _player.OnHidingUIElements += OnHidingElements;
            _player.OnRevealingUIElements += OnRevealingElements;
        }
    }

    private void OnHidingElements()
    {
        PlayerTeamIndex(_player.gameObject);

        if (_isAlly)
        {
            foreach (var image in _images)
            {
                var newImageTransparency = image.color;
                newImageTransparency.a = 1f;

                image.color = new Color(1f, 1f, 1f, newImageTransparency.a);
            }

            foreach (var text in _texts)
            {
                var newTextTransparency = text.color;
                newTextTransparency.a = 1f;
                text.color = new Color(1f, 1f, 1f, newTextTransparency.a);
            }
        }
        else
        {
            foreach (var image in _images)
            {
                var newImageTransparency = image.color;
                newImageTransparency.a = 0.0f;

                image.color = new Color(1f, 1f, 1f, newImageTransparency.a);
            }

            foreach (var text in _texts)
            {
                var newTextTransparency = text.color;
                newTextTransparency.a = 0.0f;
                text.color = new Color(1f, 1f, 1f, newTextTransparency.a);
            }
        }
    }

    private void OnRevealingElements()
    {
        if (_isAlly)
        {
            foreach (var image in _images)
            {
                var newImageTransparency = image.color;
                newImageTransparency.a = 1f;

                image.color = new Color(1f, 1f, 1f, newImageTransparency.a);
            }

            foreach (var text in _texts)
            {
                var newTextTransparency = text.color;
                newTextTransparency.a = 1f;
                text.color = new Color(1f, 1f, 1f, newTextTransparency.a);
            }
        }
        else
        {
            foreach (var image in _images)
            {
                var newImageTransparency = image.color;
                newImageTransparency.a = 1f;

                image.color = new Color(1f, 1f, 1f, newImageTransparency.a);
            }

            foreach (var text in _texts)
            {
                var newTextTransparency = text.color;
                newTextTransparency.a = 1f;
                text.color = new Color(1f, 1f, 1f, newTextTransparency.a);
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
