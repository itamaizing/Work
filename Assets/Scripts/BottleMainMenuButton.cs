using UnityEngine;
using UnityEngine.UI;

public class BottleMainMenuButton : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (BottleUserManager.Instance != null)
        {
            BottleUserManager.Instance.AddBottleVolume(0.5f);
        }
    }
}
