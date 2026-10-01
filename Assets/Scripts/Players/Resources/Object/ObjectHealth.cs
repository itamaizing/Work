using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectHealth : Resource, IDamageable, ITargetable
{
    [Header("UI / Visual")]
    [SerializeField] private ObjectBar _objectBar;
    
    [Header("Data")]
    [SerializeField] private ObjectData _objectData;
    [SerializeField] private Object obj;

    [Header("Damage type ignored")]
    [SerializeField] private List<Schools> _ignoredSchools;
    [SerializeField] private List<AbilityForm> _ignoredForms;
    [SerializeField] private List<SkillType> _ignoredSkillTypes;

    [Header("AbilityBanDatabase")]
    [SerializeField] private AbilityBanDatabase _abilityBanDatabase;
    [SerializeField] private string _selectedAbilityName;
    public AbilityBanDatabase AbilityBanDatabase => _abilityBanDatabase;

    public event Action OnDeath;

    public event Action<Damage, Skill> DamageTaken;
    //public event Action<float, Info.DamageType, Skill> DamageTakenType;

    [SyncVar] private float _maxHealth;
    [SyncVar(hook = nameof(OnHealthChanged))]
    private float _currentHealth;

    [SyncVar] private float _resistMagicDamage = 0f;

    private Coroutine _hideBarCoroutine;
    private Coroutine _regenerationCoroutine;

    [SyncVar][SerializeField] private float _regenModification = 1;

    [SerializeField] private bool live = false;
    [SerializeField] private bool isDestroyOnDeath = true;
    [SerializeField] private bool isRegenerationEnabled = false;
    public float RegenMod { get => _regenModification; set => _regenModification = value; }
    public bool IsDestroyOnDeath { get => isDestroyOnDeath; set => isDestroyOnDeath = value; }
    public ObjectData ObjectData => _objectData;
    public float ResistMagicDamage => _resistMagicDamage;
    
    private float _fillDuration;
    private float _fillTime;
    private float _fillTargetValue;

    public float CurrentHealth
    {
        get => _currentHealth;
        set => _currentHealth = value;
    }

    public bool IsRegenerationEnabled
    {
        get => isRegenerationEnabled;
        set
        {
            if (isRegenerationEnabled == value) return;

            isRegenerationEnabled = value;

            if (isRegenerationEnabled) СmdStartCustomRegeneration();
            else СmdStopCustomRegeneration();
        }
    }

    public Vector3 Position => throw new NotImplementedException();

    public Transform Transform => throw new NotImplementedException();

    public bool IsTargetable => throw new NotImplementedException();

    #region regeneration

    private Coroutine _fillCoroutine;

    private void Awake() => InitializeObject(ObjectData);

    private void OnDisable()
    {
        StopCustomRegeneration();
    }

    public void StartCustomRegeneration()
    {
        if (isRegenerationEnabled)
        {
            if (_regenerationCoroutine != null)
            {
                StopCoroutine(_regenerationCoroutine);
                _regenerationCoroutine = null;
            }

            _regenerationCoroutine = StartCoroutine(CustomRegenerationRoutine());
        }
    }

    public void StartCustomNegativeRegeneration()
    {
        if (_regenerationCoroutine != null)
        {
            StopCoroutine(_regenerationCoroutine);
            _regenerationCoroutine = null;
        }

        if (isRegenerationEnabled) _regenerationCoroutine = StartCoroutine(CustomNegativeRegenerationRoutine());
    }

    private void StopCustomRegeneration(bool immediate = false)
    {
        if (_regenerationCoroutine != null)
        {
            StopCoroutine(_regenerationCoroutine);
            _regenerationCoroutine = null;
        }

        if (immediate) StopAllCoroutines();
    }

    private void StopCustomNegativeRegeneration(bool immediate = false)
    {
        if (_regenerationCoroutine != null)
        {
            StopCoroutine(_regenerationCoroutine);
            _regenerationCoroutine = null;
        }

        if (immediate) StopAllCoroutines();
    }

    [Server]
    public void ServerStartFillHP(float targetValue, float duration)
    {
        if (_fillCoroutine != null) StopCoroutine(_fillCoroutine);
    
        _fillTargetValue = targetValue;
        _fillDuration = duration;
        _fillTime = 0f;

        _fillCoroutine = StartCoroutine(FillHPCoroutine());
    }
    
    [Server]
    public void ServerRollbackFillHP(float timeToRollback)
    {
        if (_fillCoroutine == null) return;

        _fillTime = Mathf.Max(0f, _fillTime - timeToRollback);

        float t = _fillDuration > 0 ? Mathf.Clamp01(_fillTime / _fillDuration) : 1f;
        _currentHealth = Mathf.Lerp(0f, _fillTargetValue, t);

        RpcSyncHP(_currentHealth);
    }

    [Server]
    public void ServerInterruptFillHP()
    {
        if (_fillCoroutine == null) return;

        StopCoroutine(_fillCoroutine);
        _fillCoroutine = null;

        RpcSyncHP(_currentHealth);
    }

    [ClientRpc]
    private void RpcSyncHP(float value)
    {
     _currentHealth = value;
     OnHealthChanged(0, _currentHealth);
    }

    private IEnumerator CustomRegenerationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_objectData.RegenerationInterval);

            if (_currentHealth <= 0)
                yield break;

            if (_currentHealth < MaxValue)
            {
                _currentHealth = Mathf.Min(MaxValue, _currentHealth + _objectData.RegenerationAmount * _regenModification);
                OnHealthChanged(0, _currentHealth);
            }
        }
    }

    private IEnumerator CustomNegativeRegenerationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_objectData.RegenerationInterval);

            if (_currentHealth > 0)
            {
                _currentHealth = Mathf.Max(0, _currentHealth - 1);
                OnHealthChanged(_currentHealth, _currentHealth);
            }

            if (_currentHealth <= 0)
            {
                if (isServer) NetworkServer.Destroy(gameObject);
                else Destroy(gameObject);

                yield break;
            }
        }
    }


    private IEnumerator FillHPCoroutine()
    {
        while (_fillTime < _fillDuration)
        {
            _fillTime += Time.deltaTime;
            float t = _fillDuration > 0 ? Mathf.Clamp01(_fillTime / _fillDuration) : 1f;

            float newHP = Mathf.Lerp(0f, _fillTargetValue, t);
            _currentHealth = newHP;

            OnHealthChanged(_currentHealth, _currentHealth);

            yield return null;
        }

        _currentHealth = _fillTargetValue;
        OnHealthChanged(_currentHealth, _currentHealth);

        _fillCoroutine = null;
    }

    #endregion

    #region Initialization

    private void InitializeObject(ObjectData objectData)
    {
        _objectData = objectData;

        //Initialize(objectData.MaxHealth, objectData.RegenerationAmount, objectData.RegenerationInterval, null, objectData.Attribute_old);

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

    public bool CheckIngorSkill(Skill skill)
    {
        if (IsDamageIgnored(skill)) return false;
        if (skill != null && skill.GetType().Name == _selectedAbilityName) return false;

        return true;
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        if (!CheckIngorSkill(skill)) return false;
        if (TryEvade(damage.Type)) return false;   

        if (_regenerationCoroutine == null) СmdStartCustomRegeneration();
        float damageValue = damage.Value;
        Debug.Log("0");

        if (_currentHealth > 0)
        {
            Debug.Log("1");
            _currentHealth -= damageValue;
             
            DamageTaken?.Invoke(damage, skill);
            //DamageTakenType?.Invoke(damageValue, damage.Type, skill);

            if (_objectBar != null && (_objectData == null || !_objectData.HideBar))
            {
                _objectBar.ShowHealthBar();
                _objectBar.SetHealth(_currentHealth);
            }

            if (_currentHealth <= 0)
            {
                OnDeath?.Invoke();
                if (obj != null) obj.IsDeath = true;

                GameObject target = transform.parent != null ? transform.parent.gameObject : gameObject;
                СmdStopCustomRegeneration();

                if (isDestroyOnDeath)
                {
                    if (isServer && target.TryGetComponent(out NetworkIdentity identity)) NetworkServer.Destroy(target);
                    else Destroy(target);

                }

                else
                {
                    if (isServer) ClienRpcActive(false);
                }
            }

            if (isServer) RpcPopupDamage(damage.Value);
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
        if (_objectBar == null || (_objectData != null && _objectData.HideBar)) return;

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

    public void ReplaceObjectData(ObjectData newData)
    {
        _objectData = newData;
        _maxHealth = newData.MaxHealth;

        ServerStartFillHP(_maxHealth, 0f);
    }

    private void TryUpdateBar()
    {

    }

    [Command]
    public void CmdSetCurrentHealth(float newValue)
    {
        _currentHealth = Mathf.Clamp(newValue, 0, MaxValue);
        OnHealthChanged(0, _currentHealth);
    }

    [Server]
    public void СmdStartCustomRegeneration()
    {
        StopCustomNegativeRegeneration(true);
        StartCustomRegeneration();
        ClientRpcStartCustomRegeneration();
    }

    [Server]
    public void СmdStartCustomNegativeRegeneration()
    {
        StopCustomRegeneration(true);
        StartCustomNegativeRegeneration();
        //ClientRpcStartNegaiveCustomRegeneration();
    }

    [Server]
    public void СmdStopCustomRegeneration()
    {
        StopCustomRegeneration();
        ClientRpcStopCustomRegeneration();
    }

    [Server]
    public void ServerSetCurrentHealth(float newValue)
    {
        _currentHealth = Mathf.Clamp(newValue, 0, MaxValue);
        if (obj != null) obj.IsDeath = false;
        gameObject.SetActive(true);
        RpcSyncHP(_currentHealth);
    }

    [ClientRpc]
    private void RpcPopupDamage(float value)
    {
        Damage damage = new Damage { Value = value, Type = DamageType.Physical };
        DamageTaken?.Invoke(damage, null);
    }

    [ClientRpc]
    private void ClienRpcActive(bool value)
    {
        gameObject.SetActive(value);
    }

    [ClientRpc]
    private void ClientRpcStopCustomRegeneration()
    {
        StopCustomRegeneration();
    }

    [ClientRpc]
    public void ClientRpcStartCustomRegeneration()
    {
       StartCustomRegeneration();
    }

    [ClientRpc]
    public void ClientRpcStartNegaiveCustomRegeneration()
    {
        StartCustomRegeneration();
    }


    public void ShowPhantomValue(Damage phantomValue)
    {
        throw new NotImplementedException();
    }

    private bool IsDamageIgnored(Skill skill)
    {
        if (skill == null) return false;

        if (_ignoredSchools.Contains(skill.Info.School)) return true;
        if (_ignoredForms.Contains(skill.Info.AbilityForm)) return true;
        if (_ignoredSkillTypes.Contains(skill.Targeting.SkillType)) return true;
        return false;
    }

    protected virtual void HookBonusMaxValueChanged(float oldValue, float newValue) { }
}
