using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
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
        FindObjectOfType<GameRules>().CloseRoomOnClient();
    }
}
