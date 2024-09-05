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
	[SerializeField] protected float _timeToDisapear = 0.2f;
	[SerializeField] protected float _disapearSpeed = 0.5f;
	[SerializeField] protected Slider _barPlus;
	[SerializeField] protected float _timeToShow = 0.2f;
	[SerializeField] protected float _ShowSpeed = 0.5f;
	[SerializeField] protected bool _showText = true;
	[SerializeField] protected TMP_Text _barText;

	protected float _currentValue;
	protected float _maxValue;

	public virtual void Init(Resource resource)
    {
		_resource = resource;

		_currentValue = resource.CurrentValue;
		_maxValue = resource.MaxValue;

		UpdateBar();

		_resource.ValueChanged += OnValueChanged;
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
}
