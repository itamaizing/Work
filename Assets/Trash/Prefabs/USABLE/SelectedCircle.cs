using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SelectedCircle : MonoBehaviour
{
    [SerializeField] private DecalProjector _selectProjector;
    [SerializeField] private DecalProjector _selectProjectorTargetVariant;
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
			_selectProjector.gameObject.SetActive(_isActive);
			//gameObject.SetActive(_isActive);
        }
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
        _selectProjectorTargetVariant.gameObject.SetActive(value);
		//_stroke.gameObject.SetActive(value);
    }

    public void SwitchStroke(bool value) 
    {
		_stroke.gameObject.SetActive(value);
	}

    public void SwitchSelectCircle(bool value)
    {
        _selectProjector.gameObject.SetActive(value);

        if (value && _mat != null)
        {
            _mat.color = Color.green;
        }
    }

    public void SetColorTarget(Color value) 
    {
        _mat.color = value;
    }
}
