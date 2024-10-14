using DG.Tweening;
using JetBrains.Annotations;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Bar : MonoBehaviour
{
	[CanBeNull]
	[SerializeField] private Resource _resource;

    [SerializeField] protected Slider _bar;
	[SerializeField] protected Slider _barMinus;
	[SerializeField] protected Slider _barPlus;
	[SerializeField] protected float _timeToDisapear = 0.2f;
	[SerializeField] protected float _disapearSpeed = 0.5f;
	[SerializeField] protected float _timeToShow = 0.2f;
	[SerializeField] protected float _ShowSpeed = 0.5f;
	[SerializeField] protected bool _showText = true;
	[SerializeField] protected TMP_Text _barText;

	protected float _currentValue;
	protected float _maxValue;
	protected float _preViewValue;

	public virtual void Init(Resource resource)
    {
		if(_resource != null)
        {
			_resource.ValueChanged -= OnValueChanged;
			_resource.PhantomValueShown -= PreviewChange;
			_resource.MaxValueChanged -= OnMaxValueChanged;
		}

		_resource = resource;

		_currentValue = resource.CurrentValue;
		_preViewValue = resource.CurrentValue;
		_maxValue = resource.MaxValue;

		UpdateBar();

		_resource.ValueChanged += OnValueChanged;
		_resource.PhantomValueShown += PreviewChange;
		_resource.MaxValueChanged += OnMaxValueChanged;
	}

    private void Start()
    {
        if (_resource != null)
			Init(_resource);
    }

    private void OnDestroy()
    {
		_resource.ValueChanged -= OnValueChanged;
		_resource.PhantomValueShown -= PreviewChange;
		_resource.MaxValueChanged -= OnMaxValueChanged;
	}

    public virtual void UpdateBar()
	{
		_bar.value = _currentValue / _maxValue;

		if(_showText)
			_barText.text = Mathf.RoundToInt(_currentValue).ToString();

		StartCoroutine(DisapearBar());
	}

	private void OnValueChanged(float oldValue, float newValue)
    {
        _currentValue = newValue;
		UpdateBar();
	}
    
    private void OnMaxValueChanged(float oldValue, float newValue)
    {
        _maxValue = newValue;
		UpdateBar();
	}

	private IEnumerator DisapearBar()
	{
		yield return new WaitForSeconds(_timeToDisapear);
		_barMinus.DOValue(_currentValue / _maxValue, _disapearSpeed);
	}

	public void PreviewChange(float damage)
	{
		float newValue = _currentValue - damage;
		//Debug.Log(newValue + " new " + _currentValue + " cur " + _maxValue + " max" );
		//Debug.Log(_barPlus + " name: "+ name);
		if (_barPlus != null)
		{
			if (newValue < _currentValue)
			{
				_preViewValue = newValue;

				_bar.value = _preViewValue / _maxValue;
				_barPlus.value = _currentValue / _maxValue;
			}
			else
			{
				_preViewValue = _currentValue;
				//_currentValue = newValue;

				//_bar.value = newValue / _maxValue;
				_barPlus.value = newValue / _maxValue;
			}
		}
		// fading bar
		//_currentValue 
	}
}
