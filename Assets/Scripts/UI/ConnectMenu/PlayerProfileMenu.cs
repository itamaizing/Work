using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerProfileMenu : MonoBehaviour
{
    [SerializeField] private TMP_Text _name;
    [SerializeField] private Image _ico;

    public void Init(string name, Sprite ico = null)
    {
        _name.text = name;
        if(ico != null)
            _ico.sprite = ico;
    }
}

