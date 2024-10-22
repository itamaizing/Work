using UnityEngine;

public class SelectedCircle : MonoBehaviour
{
    [SerializeField]private SpriteRenderer _circle;
    private bool _isActive;

    public SpriteRenderer Circle => _circle;

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
