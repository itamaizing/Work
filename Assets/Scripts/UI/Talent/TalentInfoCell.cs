using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TalentInfoCell : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private Image _image;

    public TMP_Text Text { get => _text; }

    public void ShowDividingLine()
    {
        _image.gameObject.SetActive(true);
    }

    public void HideDividingLine()
    {
        _image.gameObject.SetActive(false);
    }
}
