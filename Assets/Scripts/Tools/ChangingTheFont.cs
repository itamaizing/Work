using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChangingTheFont : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset _newFont;

    [ContextMenu("ChangingTheFont")]
    private void ChangAll()
    {
        TextMeshProUGUI[] tempFonts = GetComponentsInChildren<TextMeshProUGUI>();

        foreach (var item in tempFonts)
        {
            item.font = _newFont;
        }
    }
}
