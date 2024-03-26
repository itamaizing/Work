using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Players.Abilities.Genjalf
{
    public class Shield : MonoBehaviour
    {
        [SerializeField] private SoShieldData _soShieldData;
        [SerializeField] private GameObject _iconAbility;
        [SerializeField] private Toggle _toggleAbility;
        [SerializeField] private GameObject _abilitiesPanel;
        [SerializeField] private GameObject _castPrefab;
        [SerializeField] private GameObject _manaCost;


        private float _currentHealth;
        private float _currentMana;

        private Coroutine _coroutineActiveShield;
        private Coroutine _resetCoroutine;
        private int _currentShieldCharge;
        private float _currentAbAmount;
        private bool _isResetCoroutineRunning = false;

        private bool _canCast = true;
        private bool _isGlobalCooldown;
        private bool isShieldActive = false;
        private GameObject _newCastPrefab;
        private bool _isEnabled = false;
        private Coroutine _coroutine;


        private void Start()
        {
            _currentHealth = gameObject.transform.parent.GetComponent<HealthPlayer>().MaxHealth;
            _currentMana = gameObject.transform.parent.GetComponent<ManaPlayer>().Mana;
            _currentShieldCharge = _soShieldData.ShieldCharges;
        }

        private void Update()
        {
            CheckChargeOnStartReset();
            ActivatedAbility();
        }

        private void ActivatedAbility()
        {
            if (_toggleAbility.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Alpha1) &&
                transform.parent.GetComponent<PlayerMove>().IsSelect && _toggleAbility.enabled)
            {
                if (_resetCoroutine != null)
                {
                    StopCoroutine(_resetCoroutine);
                    _isResetCoroutineRunning = false;
                }

                _coroutineActiveShield = StartCoroutine(ActiveShield(_soShieldData.DurationShield));
            }

        }

        public void StartResetTime()
        {
            StartCoroutine(ResetTimeForCharging(_soShieldData.CooldownCharge));
            _isResetCoroutineRunning = true;
        }


        //Включаем щит.
        private IEnumerator ActiveShield(float durationCast)
        {
            if (_resetCoroutine != null)
            {
                StopCoroutine(_resetCoroutine);
                _isResetCoroutineRunning = false;
            }
            transform.parent.GetComponent<PlayerMove>().CanMove = false;
            
            if (!_isGlobalCooldown)
            {
                _abilitiesPanel.GetComponent<GlobalCooldown>().StartGlobalCooldown();
                _isGlobalCooldown = true;
            }

            
            _manaCost.SetActive(true);
            _manaCost.GetComponent<VisualManaCost>().CheckManaCost();
            _manaCost.transform.localScale = new Vector2(2f, _manaCost.gameObject.transform.localScale.y);

            Debug.Log($"Кастую щит");
            yield return new WaitForSeconds(durationCast);

            _currentShieldCharge--;
            Debug.Log("Конец каста щита");
            transform.parent.GetComponent<PlayerMove>().CanMove = true;
            _manaCost.SetActive(false);
            transform.parent.GetComponent<ManaPlayer>().UseMana(_soShieldData.ManaCost);
            _currentMana = gameObject.transform.parent.GetComponent<ManaPlayer>().Mana;
            Debug.Log($"Mana Genjalf: {_currentMana}");
            _currentAbAmount = _soShieldData.AbsorptionAmount;
            _isGlobalCooldown = false;
        }

        private void CheckChargeOnStartReset()
        {
            if (_currentShieldCharge < _soShieldData.ShieldCharges && !_isResetCoroutineRunning)
            {
                StartResetTime();
            }
        }

        private IEnumerator ResetTimeForCharging(float resetTime)
        {
            yield return new WaitForSeconds(resetTime);
            _currentShieldCharge++;
            _isResetCoroutineRunning = false;
        }

        public void DamageInShield(float incomingDamage)
        {
            float remainingDamage = _currentAbAmount - incomingDamage;

            if (remainingDamage <= 0)
            {
                DamageHealth(Mathf.Abs(remainingDamage));
                _currentAbAmount = 0;
            }
            else
            {
                _currentAbAmount = remainingDamage;
            }
        }

        private void DamageHealth(float remainingDamage)
        {
            _currentHealth -= remainingDamage;
            //_healthText.text = "Health: " + _currentHealth.ToString();
            if (_currentHealth <= 0)
            {
                //GameOver
            }
        }
    }
}