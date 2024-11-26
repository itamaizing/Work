using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class ObjectHealth : Resource, IDamageable
{
    [SerializeField] private ObjectBar _objectBar;

    public event Action OnDeath;
    public event Action<Damage, Skill> DamageTaken;
    public event Action<float, DamageType, Skill> DamageTakenType;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private float _currentHealth;
    private Coroutine _hideBarCoroutine;
    private Coroutine _regenerationCoroutine;
    private Coroutine _regenerationDelayCoroutine;
    private ObjectData _objectData;

    public void InitializeObject(ObjectData objectData)
    {
        Initialize(objectData.MaxHealth, objectData.RegenerationRate, 0, null);

        if (_objectBar != null)
        {
            _objectBar.SetMaxHealth(objectData.MaxHealth);
        }

        if (objectData.Endurance)
        {
            _currentHealth = objectData.MaxHealth;
            _objectBar.SetHealth(_currentHealth);
            _objectBar.HideHealthBar();
            _objectData = objectData;
        }
    }

    public void ShowPhantomValue(Damage phantomValue)
    {
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        float damageValue = damage.Value;

        if (_currentHealth > 0)
        {
            _currentHealth -= damageValue;
            DamageTakenType?.Invoke(damageValue, damage.Type, skill);

            if (_objectBar != null)
            {
                ShowAndAutoHideBar();
                _objectBar.SetHealth(_currentHealth);
            }

            if (_currentHealth <= 0)
            {
                OnDeath?.Invoke();
                Destroy(gameObject);
            }

            if (_regenerationCoroutine != null)
            {
                StopCoroutine(_regenerationCoroutine);
                _regenerationCoroutine = null;
            }

            if (_regenerationDelayCoroutine != null)
            {
                StopCoroutine(_regenerationDelayCoroutine);
            }
            _regenerationDelayCoroutine = StartCoroutine(StartRegenerationWithDelay(2f));

            return true;
        }
        return false;
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        if (_objectBar != null)
        {
            ShowAndAutoHideBar();
            _objectBar.SetHealth(newHealth);
        }
    }

    private void ShowAndAutoHideBar()
    {
        if (_objectBar != null)
        {
            _objectBar.ShowHealthBar();

            if (_hideBarCoroutine != null)
            {
                StopCoroutine(_hideBarCoroutine);
            }

            _hideBarCoroutine = StartCoroutine(HideHealthBarAfterDelay(2f));
        }
    }

    public void SetCurrentHealth(float value)
    {
        _currentHealth += value;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, MaxValue);
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (_objectBar != null)
        {
            ShowAndAutoHideBar();
            _objectBar.SetHealth(_currentHealth);
        }
    }

    private IEnumerator HideHealthBarAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _objectBar.HideHealthBar();
    }

    private IEnumerator StartRegenerationWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        _regenerationCoroutine = StartCoroutine(RegenerateHealth());
    }

    private IEnumerator RegenerateHealth()
    {
        while (_currentHealth < MaxValue)
        {
            _currentHealth += _objectData.RegenerationRate * Time.deltaTime;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, MaxValue);

            if (_objectBar != null)
            {
                _objectBar.SetHealth(_currentHealth);
            }

            yield return null;
        }
    }
}
