using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class ObjectHealth : Resource, IDamageable
{
    [Header("UI / Visual")]
    [SerializeField] private ObjectBar _objectBar;

    [Header("Data")]
    [SerializeField] private ObjectData _objectData;

    public event Action OnDeath;
    public event Action FullyRegenerated;

    public event Action<Damage, Skill> DamageTaken;
    //public event Action<float, DamageType, Skill> DamageTakenType;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private float _currentHealth;

    [SyncVar] private float _resistMagicDamage = 0f;

    private Coroutine _hideBarCoroutine;
    private Coroutine _regenerationCoroutine;
    private Coroutine _regenerationDelayCoroutine;

    public ObjectData ObjectData => _objectData;
    public float ResistMagicDamage => _resistMagicDamage;

    public float CurrentHealth
    {
        get => _currentHealth;
        set => _currentHealth = value;
    }

    #region regeneration

    private Coroutine _fillCoroutine;

    [Server]
    public void ServerStartFillHP(float targetValue, float duration)
    {
        if (_fillCoroutine != null)
        {
            StopCoroutine(_fillCoroutine);
        }
        _fillCoroutine = StartCoroutine(FillHPCoroutine(targetValue, duration));
    }

    [Server]
    public void ServerStopFillHP()
    {
        if (_fillCoroutine != null)
        {
            StopCoroutine(_fillCoroutine);
            _fillCoroutine = null;
        }
    }

    private IEnumerator FillHPCoroutine(float targetValue, float duration)
    {
        float startValue = _currentHealth;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            float newHP = Mathf.Lerp(startValue, targetValue, t);
            _currentHealth = newHP;

            OnHealthChanged(_currentHealth, _currentHealth);

            yield return null;
        }

        _currentHealth = targetValue;
        OnHealthChanged(_currentHealth, _currentHealth);

        _fillCoroutine = null;
        if (Mathf.Approximately(_currentHealth, ObjectData.MaxHealth)) FullyRegenerated?.Invoke();
    }

    #endregion

    #region Initialization

    public void InitializeObject(ObjectData objectData)
    {
        _objectData = objectData;

        Initialize(objectData.MaxHealth, objectData.RegenerationRate, 0, null);

        if (objectData.MaxEndurance)
        {
            _currentHealth = objectData.MaxHealth;
            ValuesObjectData(objectData);
        }

        else if (objectData.MinEndurance)
        {
            _currentHealth = 0;
            ValuesObjectData(objectData);
        }
    }

    private void ValuesObjectData(ObjectData objectData)
    {
        if (_objectBar != null)
        {
            _objectBar.SetMaxHealth(objectData.MaxHealth);
            _objectBar.SetHealth(_currentHealth);
            _objectBar.HideHealthBar();
        }
    }

    #endregion

    #region Take Damage

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        if (TryEvade(damage.Type))
        {
            return false;
        }

        float damageValue = damage.Value;

        if (_currentHealth > 0)
        {
            _currentHealth -= damageValue;
             
            DamageTaken?.Invoke(damage, skill);
            //DamageTakenType?.Invoke(damageValue, damage.Type, skill);

            if (_objectBar != null)
            {
                _objectBar.ShowHealthBar();
                _objectBar.SetHealth(_currentHealth);
            }

            if (_currentHealth <= 0)
            {
                OnDeath?.Invoke();
                Destroy(gameObject);
            }

            return true;
        }
        return false;
    }

    private bool TryEvade(DamageType damageType)
    {
        if (damageType == DamageType.Magical)
        {
            float roll = UnityEngine.Random.Range(0, 100);
            if (roll < _resistMagicDamage) return true;
        }
        return false;
    }

    #endregion

    #region UI / Visual

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        if (_objectBar == null) return;

        _objectBar.SetHealth(newHealth);

        if (!Mathf.Approximately(newHealth, ObjectData.MaxHealth))
        {
            _objectBar.ShowHealthBar();

            if (_hideBarCoroutine != null)
            {
                StopCoroutine(_hideBarCoroutine);
                _hideBarCoroutine = null;
            }
        }
        else
        {
            if (_hideBarCoroutine != null)
            {
                StopCoroutine(_hideBarCoroutine);
                _hideBarCoroutine = null;
            }

            _objectBar.HideHealthBar();
        }
    }

    //private void ShowAndAutoHideBar()
    //{
    //    if (_objectBar == null) return;

    //    _objectBar.ShowHealthBar();

    //    if (_hideBarCoroutine != null)
    //        StopCoroutine(_hideBarCoroutine);

    //    _hideBarCoroutine = StartCoroutine(HideHealthBarAfterDelay(2f));
    //}

    //private IEnumerator HideHealthBarAfterDelay(float delay)
    //{
    //    yield return new WaitForSeconds(delay);

    //    if (_fillCoroutine == null)
    //        _objectBar.HideHealthBar();
    //}

    public void SetMagicEvade(float value)
    {
        _resistMagicDamage = Mathf.Clamp(value, 0f, 100f);
    }

    #endregion

    [Command]
    public void CmdSetCurrentHealth(float newValue)
    {
        _currentHealth = Mathf.Clamp(newValue, 0, MaxValue);
        OnHealthChanged(_currentHealth, _currentHealth);
    }

    public void ShowPhantomValue(Damage phantomValue)
    {
        throw new NotImplementedException();
    }
}
