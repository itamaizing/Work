using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttributePanel : MonoBehaviour
{
    [SerializeField] private GameObject _content;
    [SerializeField] private AttributeItem[] _attributes;


    public void SwitchVisible(bool visible)
    {
        _content.SetActive(visible);
    }
}
