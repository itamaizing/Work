using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEditor;

public class ChangingTheFont : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset _newFont;

    [ContextMenu("ChangingTheFont")]
    private void ChangAll()
    {
        TextMeshProUGUI[] tempFonts = GetComponentsInChildren<TextMeshProUGUI>();

        foreach (var item in tempFonts)
        {
            Undo.RecordObject(item, "font");
            item.font = _newFont;
        }
    }
}
