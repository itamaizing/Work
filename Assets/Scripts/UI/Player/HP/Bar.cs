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
    [SerializeField] private Slider _shieldBar;
    [SerializeField] protected Slider _bar;
    [SerializeField] protected Slider _barMinus;
    [SerializeField] protected Slider _barPlus;
    [SerializeField] protected float _timeToDisappear = 0.2f;
    [SerializeField] protected float _disappearSpeed = 0.5f;
    [SerializeField] protected float _timeToShow = 0.2f;
    [SerializeField] protected float _showSpeed = 0.5f;
    [SerializeField] protected bool _showText = true;
    [SerializeField] protected TMP_Text _barText;

    protected float _currentValue;
    protected float _maxValue;
    protected float _preViewValue;
    private Health _health;

    public virtual void Init(Resource resource)
    {
        if (_resource != null)
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

        _health = resource as Health;
        if (_health != null)
        {
            _health.OnShieldValuesChanged += UpdateShieldBar;
        }

        if (_shieldBar != null)
        {
            UpdateShieldVisual();
        }
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

        if (_health != null)
        {
            _health.OnShieldValuesChanged -= UpdateShieldBar;
        }
    }

    public virtual void UpdateBar()
    {
        // Обновляем значение основной полоски в зависимости от текущего здоровья
        _bar.value = _currentValue / _maxValue;

        if (_showText)
            _barText.text = Mathf.RoundToInt(_currentValue).ToString();

        StartCoroutine(DisappearBar());
    }

    private void OnValueChanged(float oldValue, float newValue)
    {
        _currentValue = newValue;
        UpdateBar();
        if (_shieldBar != null) UpdateShieldVisual();
    }

    private void OnMaxValueChanged(float oldValue, float newValue)
    {
        _maxValue = newValue;
        UpdateBar();
        if (_shieldBar != null) UpdateShieldVisual();
    }

    private IEnumerator DisappearBar()
    {
        yield return new WaitForSeconds(_timeToDisappear);
        _barMinus.DOValue(_currentValue / _maxValue, _disappearSpeed);
    }

    public void PreviewChange(float damage)
    {
        float newValue = _currentValue - damage;
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
                _barPlus.value = newValue / _maxValue;
            }
        }
    }

    private void UpdateShieldBar(float absorbed, float maxAbsorption)
    {
        if (_shieldBar != null)
        {
            float healthBarTarget = (_currentValue - (maxAbsorption - absorbed)) / _maxValue;
            _bar.DOValue(healthBarTarget, _disappearSpeed);
            _barMinus.DOValue(healthBarTarget, _disappearSpeed);
        }
    }

    private void UpdateShieldVisual()
    {
        if (_shieldBar != null)
        {
            _shieldBar.value = _currentValue / _maxValue;
        }
    }
}
