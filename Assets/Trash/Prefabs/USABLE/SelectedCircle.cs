using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SelectedCircle : MonoBehaviour
{
    [SerializeField] private DecalProjector _selectProjector;
    [SerializeField] private DecalProjector _stroke;

    private bool _isActive;
    private Material _mat;

    public bool IsActive
    {
        get
        {
            return _isActive;
        }
        set
        {
            _isActive = value;
            if (_selectProjector != null) _selectProjector.gameObject.SetActive(_isActive);
            //gameObject.SetActive(_isActive);
        }
    }

    private void OnDestroy()
    {
        _isActive = false;
        if (_selectProjector != null) _selectProjector.gameObject.SetActive(false);
        if (_stroke != null) _stroke.gameObject.SetActive(false);
    }

    private void Start()
	{
		_mat = Instantiate(_selectProjector.material);
		_selectProjector.material = _mat;
		//mat.SetFloat("_GrayscaleAmount", grey);

		//_closestTargetProjector.material
	}

	public void SwitchClostestTarget(bool value)
    {
        _selectProjector.gameObject.SetActive(value);
		//_stroke.gameObject.SetActive(value);
    }

    public void SwitchStroke(bool value) 
    {
		_stroke.gameObject.SetActive(value);
	}

    public void SetColorTarget(Color value) 
    {
        _mat.color = value;
    }
}
