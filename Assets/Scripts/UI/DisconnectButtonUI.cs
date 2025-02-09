using Mirror;
using System.Collections;
using System.Collections.Generic;
using Telepathy;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DisconnectButtonUI : MonoBehaviour
{
    [SerializeField] private Button _button;

    private void OnValidate()
    {
        _button = gameObject.GetComponent<Button>();
    }

    private void Awake()
    {
        _button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        var gameRules = FindObjectOfType<GameRules>();

        if (gameRules != null)
        {
            gameRules.CloseRoomOnClient();
        }
        else
        {
            HeroComponent hero = FindObjectOfType<HeroComponent>();
            var roomName = hero.NetworkSettings.RoomName;
            hero.DestroySelf();
            ServerManager.Instance.EnableMenu();
            SceneManager.UnloadSceneAsync(roomName);
        }
    }
}
