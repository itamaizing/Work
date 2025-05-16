using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapMarker : MonoBehaviour
{
    private bool _isActive;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (this == null || gameObject == null) return;
            _isActive = value;
            gameObject.SetActive(_isActive);
        }
    }
}
