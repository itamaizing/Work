using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public abstract class Ability : MonoBehaviour
{
    [SerializeField] private KeyCode _activationButton;

    [Header("Abilities panel")]
    [SerializeField] private GameObject _iconAbility;
    [SerializeField] private Toggle _toggleAbility;
    [SerializeField] private GameObject _abilitiesPanel;
    [Header("Mana")]
    [SerializeField] private ManaPlayer _mana;
    [SerializeField] private float _manaCost = 0f;
    [Header("Charges")]
    [SerializeField] private bool _isUseCharges;
    [SerializeField] private TextMeshProUGUI _currentChargeText;
    [SerializeField] private int _maxCharges;
    [SerializeField] private float _chargeCooldown;

    private int _currentChargers;
    private Coroutine _rechargeJob;

    private bool _isReady = true;

    public int Chargers => _currentChargers;
    public bool IsHaveCharge { get => (_currentChargers > 0); private set { } }
    public bool IsReady { get => _isReady; set => _isReady = value; }

    protected virtual void Start()
    {
        if (_isUseCharges)
        {
            _currentChargers = _maxCharges;
            SetCurrentChargeText(_currentChargers);
        }
    }

    protected virtual void Update()
    {
        TryActivatedAbility();
    }

    protected virtual void TryActivatedAbility()
    {
        if ((_toggleAbility.gameObject.activeSelf && Input.GetKeyDown(_activationButton) && 
            transform.parent.GetComponent<PlayerMove>().IsSelect && _toggleAbility.enabled &&
            _mana.Mana >= _manaCost && IsReady) == false)
            return;

        if (_isUseCharges)
        {
            if (TryUseCharge() == false)
                return;
        }
        _mana.UseMana(_manaCost);

        Use();
    }

    public abstract void Use();

    protected bool TryUseCharge()
    {
        if (_currentChargers > 0)
        {
            _currentChargers--;
            SetCurrentChargeText(_currentChargers);

            if (_rechargeJob == null)
                _rechargeJob = StartCoroutine(RechargeJob());
            return true;
        }
        else
        {
            return false;
        }
    }

    private void SetCurrentChargeText(int value)
    {
        if (value > 0)
            _currentChargeText.color = Color.green;
        else
            _currentChargeText.color = Color.red;

        _currentChargeText.text = value.ToString();
    }

    private IEnumerator RechargeJob()
    {
        while (_currentChargers < _maxCharges)
        {
            float time = 0;
            while (time < _chargeCooldown)
            {
                time += Time.deltaTime;
                yield return null;
            }
            _currentChargers++;
            SetCurrentChargeText(_currentChargers);
        }
        _rechargeJob = null;
    }
}
