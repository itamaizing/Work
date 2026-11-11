using DG.Tweening;
using JetBrains.Annotations;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Bar : MonoBehaviour
{
	private const string ScalePrefabName = "WorldResourceBarScale";
	private const int MaxTicksPerRange = 5000;
	private static GameObject _scalePrefab;

	[CanBeNull]
	[SerializeField] private Resource _resource;
	[SerializeField] private Slider _shieldBar;
	[SerializeField] protected Slider _bar;
	[SerializeField] protected Slider _barMinus;
	//[SerializeField] protected Slider _barMinusPrewiew;
	[SerializeField] protected Slider _barPlus;
	[SerializeField] protected float _timeToDisapear = 0.2f;
	[SerializeField] protected float _disapearSpeed = 0.5f;
	[SerializeField] protected float _timeToShow = 0.2f;
	[SerializeField] protected float _ShowSpeed = 0.5f;
	[SerializeField] protected bool _showText = true;
	[SerializeField] protected TMP_Text _barText;
	[SerializeField] protected Image _barImage;

	[Header("World bar ticks")]
	[SerializeField, Min(1)] private int _minorTickStep = 100;
	[SerializeField, Min(1)] private int _majorTickStep = 1000;
	[SerializeField, Min(1)] private int _psiTickStep = 10;
	[SerializeField] private Color _tickColor = Color.black;

	protected float _currentValue;
	protected float _healthBarTarget;
	protected float _maxValue;
	protected float _preViewValue;
	private Health _health;
	private CanvasRenderer _scaleRenderer;
	private Mesh _scaleMesh;
	private Vector2 _scaleHeightRange = new Vector2(0f, 1f);
	private float _psiThreshold;
	private float _psiWidth;
	private (Rect bounds, float maximum, float psiThreshold, float psiWidth, int minorStep, int majorStep, int psiStep, Color color) _renderedScale;

	private bool _isDestroyed = false;
	private bool ShieldActive = false;

	public virtual void Init(Resource resource)
    {
		if(_resource != null)
        {
			_resource.ValueChanged -= OnValueChanged;
			_resource.PhantomValueShown -= PreviewChange;
			_resource.MaxValueChanged -= OnMaxValueChanged;
			_resource.ChangedBarColor -= OnChangeBarColor;
		}

		_resource = resource;

		_currentValue = resource.CurrentValue;
		_preViewValue = resource.CurrentValue;
		_maxValue = resource.MaxValue;
		CreateResourceScale();

		UpdateBar();

		_resource.ValueChanged += OnValueChanged;
		_resource.PhantomValueShown += PreviewChange;
		_resource.MaxValueChanged += OnMaxValueChanged;
		_resource.ChangedBarColor += OnChangeBarColor;

		_health = resource as Health;
		if (_health != null)
		{
			_health.ShieldDeactivated += OnShieldDeactivated;
			_health.OnShieldValuesChanged += UpdateShieldBar;
		}

		if (_shieldBar != null)
		{
			UpdateShieldVisual();
		}
	}

    private void Start()
    {
        if (_resource != null) Init(_resource);
    }

    private void OnEnable()
    {
		if (_scaleRenderer != null)
		{
			_renderedScale = default;
			_scaleRenderer.gameObject.SetActive(true);
		}

		if (_resource != null) Init(_resource);
	}

	private void OnDisable()
	{
		if (_scaleRenderer != null)
			_scaleRenderer.gameObject.SetActive(false);
	}

    private void OnDestroy()
    {
	    _isDestroyed = true;
		if (_scaleRenderer != null)
			Destroy(_scaleRenderer.gameObject);
		if (_scaleMesh != null)
			Destroy(_scaleMesh);
	    
		if (_resource != null)
		{
			_resource.ValueChanged -= OnValueChanged;
			_resource.PhantomValueShown -= PreviewChange;
			_resource.MaxValueChanged -= OnMaxValueChanged;
			_resource.ChangedBarColor -= OnChangeBarColor;
		}
		if (_health != null)
		{
			_health.ShieldDeactivated -= OnShieldDeactivated;
			_health.OnShieldValuesChanged -= UpdateShieldBar;
		}
	}

	/*private void Update()
	{
		if(Input.GetKeyDown(KeyCode.O)) OnChangeBarColor(Color.red);
	}*/

	public virtual void UpdateBarWithShield(float healthBarTarget)
	{
		//_bar.value = _healthBarTarget;
		_bar.DOValue(_healthBarTarget, _disapearSpeed);

		if (_showText) _barText.text = Mathf.RoundToInt(_currentValue).ToString();
		if (gameObject.activeInHierarchy) StartCoroutine(DisapearBar());
	}

	public virtual void UpdateBar()
	{
		if(_maxValue != 0)
			//_bar.value = _currentValue / _maxValue;
			_bar.DOValue(_currentValue / _maxValue, _disapearSpeed);

		if(_showText) _barText.text = Mathf.RoundToInt(_currentValue).ToString();
		
		if (_isDestroyed || _bar == null) return;
		
		if (gameObject.activeInHierarchy) StartCoroutine(DisapearBar());
	}

	private void OnValueChanged(float oldValue, float newValue)
    {
        _currentValue = newValue;

		if (_health != null && _health.IsDot) _bar.DOValue(_currentValue / _maxValue, _disapearSpeed);

		if (_shieldBar != null)
		{
			UpdateShieldVisual();
			if (ShieldActive) UpdateBarWithShield(_healthBarTarget);
			else UpdateBar();
		}

		else UpdateBar();
	}
    
    private void OnMaxValueChanged(float oldValue, float newValue)
    {
        _maxValue = newValue;

		if (_shieldBar != null)
		{
			UpdateShieldVisual();
			if (ShieldActive) UpdateBarWithShield(_healthBarTarget);
			else UpdateBar();
		}

		UpdateBar();
	}

	private void CreateResourceScale()
	{
		if (_scaleRenderer != null || _bar == null || _bar.fillRect == null ||
			_bar.GetComponentInParent<Canvas>(true)?.rootCanvas.renderMode != RenderMode.WorldSpace ||
			_bar.fillRect.parent is not RectTransform track)
			return;

		var scaleRect = CreateScale(track);
		scaleRect.offsetMin = _bar.fillRect.offsetMin;
		scaleRect.offsetMax = _bar.fillRect.offsetMax;
	}

	public float UpdatePsiScale(float current, float maximum, float threshold, float fineWidth)
	{
		if (_scaleRenderer == null)
		{
			if (_bar == null)
				_bar = GetComponent<Slider>();

			var sliderRect = (RectTransform)_bar.transform;
			var track = (RectTransform)_bar.fillRect.parent;
			var scaleRect = CreateScale(sliderRect.parent);
			scaleRect.anchorMin = sliderRect.anchorMin;
			scaleRect.anchorMax = sliderRect.anchorMax;
			scaleRect.pivot = sliderRect.pivot;
			scaleRect.sizeDelta = sliderRect.sizeDelta;
			scaleRect.anchoredPosition3D = sliderRect.anchoredPosition3D;
			scaleRect.localRotation = sliderRect.localRotation;
			scaleRect.localScale = sliderRect.localScale;
			_scaleHeightRange = new Vector2(track.anchorMin.y, track.anchorMax.y);
			_barText = scaleRect.GetComponentInChildren<TMP_Text>(true);
			if (_barText != null)
			{
				_barText.SetText("{0:0}", Mathf.RoundToInt(current));
				_barText.gameObject.SetActive(_showText);
			}
		}

		_maxValue = maximum;
		_psiThreshold = Mathf.Clamp(threshold, 0f, Mathf.Max(0f, maximum));
		_psiWidth = _psiThreshold >= maximum ? 1f : fineWidth;
		_bar.value = GetScaleFill(current);

		if (_showText && _barText != null && Mathf.RoundToInt(current) != Mathf.RoundToInt(_currentValue))
			_barText.SetText("{0:0}", Mathf.RoundToInt(current));
		_currentValue = current;
		return GetScaleFill(_psiThreshold);
	}

	private RectTransform CreateScale(Transform parent)
	{
		if (_scalePrefab == null)
			_scalePrefab = Resources.Load<GameObject>(ScalePrefabName);

		var instance = Instantiate(_scalePrefab, parent, false);
		instance.layer = parent.gameObject.layer;
		_scaleRenderer = instance.GetComponent<CanvasRenderer>();
		_scaleRenderer.SetMaterial(Graphic.defaultGraphicMaterial, Texture2D.whiteTexture);
		_scaleMesh = new Mesh { name = "Resource bar ticks" };
		return (RectTransform)instance.transform;
	}

	private void LateUpdate()
	{
		if (_scaleRenderer == null) return;

		Rect bounds = ((RectTransform)_scaleRenderer.transform).rect;
		var layout = (bounds, _maxValue, _psiThreshold, _psiWidth, _minorTickStep, _majorTickStep, _psiTickStep, _tickColor);
		if (_renderedScale == layout) return;

		_renderedScale = layout;
		using var vertices = new VertexHelper();
		PopulateScale(vertices, bounds);
		vertices.FillMesh(_scaleMesh);
		_scaleRenderer.SetMesh(_scaleMesh);
	}

	private float GetScaleFill(float value)
	{
		if (_maxValue <= 0f) return 0f;

		value = Mathf.Clamp(value, 0f, _maxValue);
		if (_psiThreshold <= 0f)
			return value / _maxValue;

		if (value <= _psiThreshold)
			return value / _psiThreshold * _psiWidth;

		return _psiWidth + (value - _psiThreshold) / (_maxValue - _psiThreshold) * (1f - _psiWidth);
	}

	private void PopulateScale(VertexHelper mesh, Rect bounds)
	{
		if (_maxValue <= 0f || bounds.width <= 0f || bounds.height <= 0f) return;

		Rect track = Rect.MinMaxRect(bounds.xMin, bounds.yMin + bounds.height * _scaleHeightRange.x,
			bounds.xMax, bounds.yMin + bounds.height * _scaleHeightRange.y);
		float border = track.height * 0.065f;
		Color32 edge = _tickColor;
		Color32 tick = new Color(_tickColor.r, _tickColor.g, _tickColor.b, _tickColor.a * 0.65f);

		AddQuad(mesh, Rect.MinMaxRect(track.xMin, track.yMin, track.xMax, track.yMin + border), edge);
		AddQuad(mesh, Rect.MinMaxRect(track.xMin, track.yMax - border, track.xMax, track.yMax), edge);
		AddDivider(mesh, track, 0f, border, 1f, edge);
		AddDivider(mesh, track, _maxValue, border, 1f, edge);

		int majorStep = Mathf.Max(1, _majorTickStep);
		AddTicks(mesh, track, _psiThreshold, _maxValue, majorStep, border, 1f, edge);
		AddTicks(mesh, track, _psiThreshold, _maxValue, _minorTickStep, border * 0.55f, 0.45f, tick, majorStep);

		if (_psiThreshold <= 0f) return;

		AddTicks(mesh, track, 0f, _psiThreshold, _psiTickStep, border * 0.55f, 0.45f, tick);
		if (_psiThreshold < _maxValue)
			AddDivider(mesh, track, _psiThreshold, border, 1f, edge);
	}

	private void AddTicks(VertexHelper mesh, Rect track, float start, float end, int step,
		float maxWidth, float height, Color32 tint, int skipStep = 0)
	{
		if (start >= end) return;

		step = Mathf.Max(1, step);
		int first = Mathf.FloorToInt(start / step) + 1;
		int last = Mathf.CeilToInt(end / step) - 1;
		int stride = Mathf.Max(1, Mathf.CeilToInt((last - first + 1f) / MaxTicksPerRange));
		float spacing = track.width * (GetScaleFill(Mathf.Min(end, start + step)) - GetScaleFill(start));
		float width = Mathf.Min(maxWidth, spacing * 0.5f);

		for (int index = first; index <= last; index += stride)
		{
			float value = index * (float)step;
			if (skipStep == 0 || value % skipStep != 0f)
				AddDivider(mesh, track, value, width, height, tint);
		}
	}

	private void AddDivider(VertexHelper mesh, Rect track, float value, float width, float height, Color32 tint)
	{
		float x = track.xMin + track.width * GetScaleFill(value);
		AddQuad(mesh, Rect.MinMaxRect(Mathf.Max(track.xMin, x - width * 0.5f), track.yMax - track.height * height,
			Mathf.Min(track.xMax, x + width * 0.5f), track.yMax), tint);
	}

	private static void AddQuad(VertexHelper mesh, Rect rect, Color32 tint)
	{
		int first = mesh.currentVertCount;
		mesh.AddVert(new Vector2(rect.xMin, rect.yMin), tint, Vector2.zero);
		mesh.AddVert(new Vector2(rect.xMin, rect.yMax), tint, Vector2.zero);
		mesh.AddVert(new Vector2(rect.xMax, rect.yMax), tint, Vector2.zero);
		mesh.AddVert(new Vector2(rect.xMax, rect.yMin), tint, Vector2.zero);
		mesh.AddTriangle(first, first + 1, first + 2);
		mesh.AddTriangle(first + 2, first + 3, first);
	}

	private IEnumerator DisapearBar()
	{
		yield return new WaitForSeconds(_timeToDisapear);
		if (ShieldActive)
        {
			_barMinus.DOValue(_healthBarTarget, _disapearSpeed);
		}

		else
		{
			//_barMinusPrewiew.DOValue(_currentValue / _maxValue, _disapearSpeed);
			_barMinus.DOValue(_currentValue / _maxValue, _disapearSpeed);
		}
	}

	public void PreviewDoTTick(float tickDamage)
	{
		float previewTarget = (_barMinus.value * _maxValue - tickDamage) / _maxValue;
		//float previewTarget = (_barMinusPrewiew.value * _maxValue - tickDamage) / _maxValue;
		previewTarget = Mathf.Clamp01(previewTarget);

		//_barMinusPrewiew.DOValue(previewTarget, 0.25f);
		_barMinus.DOValue(previewTarget, 0.25f);
	}

	public void PreviewChange(float damage)
	{
		//Debug.Log("Try phantom value");
		float newValue = _currentValue - damage;
		//Debug.Log(newValue + " new " + _currentValue + " cur " + _maxValue + " max" );
		if (_barPlus != null)
		{
			if (newValue < _currentValue)
			{
				_preViewValue = newValue;

				_bar.value = _preViewValue / _maxValue;
				_barPlus.value = _currentValue / _maxValue;

				_barMinus.gameObject.SetActive(false);
			}
			else
			{
				_preViewValue = _currentValue;
				//_currentValue = newValue;

				_bar.value = _currentValue / _maxValue;
				_barPlus.value = newValue / _maxValue;

				_barMinus.gameObject.SetActive(true);
			}
		}
	}

	private void UpdateShieldBar(float absorbed, float maxAbsorption)
	{
		if (_shieldBar != null)
		{
			if (absorbed < maxAbsorption)
			{
				_healthBarTarget = (_currentValue - (maxAbsorption - absorbed)) / _maxValue;
				_bar.DOValue(_healthBarTarget, _disapearSpeed);
				_barMinus.DOValue(_healthBarTarget, _disapearSpeed);
				//_barMinusPrewiew.DOValue(_healthBarTarget, _disapearSpeed);

				ShieldActive = true;
			}

			else
			{
				ShieldActive = false;
				UpdateBar();
			}
		}
	}

	private void UpdateShieldVisual()
	{
		if (_shieldBar != null)
		{
			_shieldBar.value = _currentValue / _maxValue;
		}
	}

	private void OnShieldDeactivated()
	{
		ShieldActive = false;
		UpdateBar();
	}

	private void OnChangeBarColor(Color color)
	{
		if(_barImage != null)
			_barImage.color = color;
	}
}
