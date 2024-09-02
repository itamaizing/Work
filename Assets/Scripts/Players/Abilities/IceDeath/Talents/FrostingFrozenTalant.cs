using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostingFrozenTalant : MonoBehaviour
{
    private bool _isActive = false;
    public bool IsActive { get { return _isActive; } }
    public void SetActive(bool active)
    {
        _isActive = active;
    }
}
