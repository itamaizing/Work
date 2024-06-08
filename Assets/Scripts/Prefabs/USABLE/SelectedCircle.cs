using UnityEngine;

public class SelectedCircle : MonoBehaviour
{
    private bool _isActive;

    public bool IsActive
    {
        get
        {
            return _isActive;
        }
        set
        {
            _isActive = value;
            gameObject.SetActive(_isActive);
        }
    }
}
