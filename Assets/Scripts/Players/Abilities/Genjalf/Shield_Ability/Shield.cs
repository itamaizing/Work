using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Players.Abilities.Genjalf.Shield_Ability
{
    public class Shield : MonoBehaviour
    {
        [SerializeField] public GameObject _cooldownButton;
        [SerializeField] private SoShieldData _soShieldData;
        [SerializeField] private GameObject _iconAbility;
        [SerializeField] private Toggle _toggleAbility;
        [SerializeField] private GameObject _abilitiesPanel;
        [SerializeField] private GameObject _castPrefab;
        [SerializeField] private GameObject _manaCost;
        [SerializeField] private ShieldBar _shieldBar;

        private UIShield _uiShield;
        private float _currentHealth;
        private float _currentMana;
        private float _currentTimeReset;
        private float _startSpeedPlayer;
        
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
            _uiShield = GetComponent<UIShield>();
            
            _startSpeedPlayer = gameObject.transform.parent.GetComponent<CharacterData>().MoveSpeed;
            _currentMana = gameObject.transform.parent.GetComponent<Mana>().Value;
            _currentShieldCharge = _soShieldData.ShieldCharges;
        }

        private void Update()
        {
            if (_shieldBar.transform.gameObject.activeSelf)
            {
                _shieldBar.SetShieldValue(_currentAbAmount);
            }

            UpdateTextCharge();
            ActivatedAbility();
        }

        private void ActivatedAbility()
        {
            if (_currentShieldCharge <= 0)
                return;

            if (_toggleAbility.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Alpha1) &&
                transform.parent.GetComponent<MoveComponent>().IsSelect && _toggleAbility.enabled)
            {
                if (_resetCoroutine != null)
                {
                    StopCoroutine(_resetCoroutine);
                    _isResetCoroutineRunning = false;
                }

                _coroutineActiveShield = StartCoroutine(ActiveShield(_soShieldData.DurationShield));
                CheckChargeOnStartReset();
            }
        }

        public void StartResetTime()
        {
            StartCoroutine(ResetTimeForCharging(_soShieldData.CooldownCharge));
            StartCoroutine(Recharge());
            _isResetCoroutineRunning = true;
        }

        private void UpdateTextCharge()
        {
            _uiShield.SetTextCharge(_currentShieldCharge);
            if(_currentShieldCharge > 0)
            {
                _uiShield.SetTextColor(Color.green);
            }
            else
            {
                _uiShield.SetTextColor(Color.red);
            }

        }

        //Включаем щит.
        private IEnumerator ActiveShield(float duration)
        {
            if (_currentShieldCharge == 0)
                yield break;

            if (_resetCoroutine != null)
            {
                StopCoroutine(_resetCoroutine);
                _isResetCoroutineRunning = false;
            }

            gameObject.transform.parent.GetComponent<MoveComponent>().SetMoveSpeed(0);

            if (!_isGlobalCooldown)
            {
                _abilitiesPanel.GetComponent<GlobalCooldown>().StartGlobalCooldown();
                _isGlobalCooldown = true;
            }

            _iconAbility.GetComponent<SpriteRenderer>().enabled = true;
            _manaCost.SetActive(true);
            _manaCost.GetComponent<VisualManaCost>().CheckManaCost();
            _manaCost.transform.localScale = new Vector2(2f, _manaCost.gameObject.transform.localScale.y);

            _currentShieldCharge--;

            transform.parent.GetComponent<MoveComponent>().SetDefaultSpeed();
            _manaCost.SetActive(false);
            transform.parent.GetComponent<Mana>().Use(_soShieldData.ManaCost);
            _currentMana = gameObject.transform.parent.GetComponent<Mana>().Value;

            _currentAbAmount = _soShieldData.AbsorptionAmount;
            _shieldBar.transform.gameObject.SetActive(true);
            _shieldBar.SetMaxValueShield(_soShieldData.AbsorptionAmount);

            _iconAbility.GetComponent<SpriteRenderer>().enabled = false;
            _isGlobalCooldown = false;

            yield return new WaitForSeconds(duration);

            _currentAbAmount = 0;
            _shieldBar.transform.gameObject.SetActive(false);

        }

        private void CheckChargeOnStartReset()
        {
            if (_currentShieldCharge < _soShieldData.ShieldCharges)
            {
                StartResetTime();
            }
        }

        private IEnumerator ResetTimeForCharging(float resetTime)
        {
            float timer = 0f;

            while (timer < resetTime)
            {
                timer += Time.deltaTime;
                _uiShield.SetTextResetTime(resetTime - timer);
                yield return null;
            }

            _currentShieldCharge++;
            _isResetCoroutineRunning = false;
        }

        public void DamageInShield(float incomingDamage)
        {
            float remainingDamage = _currentAbAmount - incomingDamage;


            if (remainingDamage <= 0)
            {
                _shieldBar.transform.gameObject.SetActive(false);
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
            _uiShield.SetterHealthUI(remainingDamage, _currentHealth);
            
            if (_currentHealth <= 0)
            {
                Destroy(gameObject.transform.parent);
            }
        }
        private IEnumerator Recharge()
        {

            // ToggleAbility.isOn = false;
            // ToggleAbility.enabled = false;


            _cooldownButton.gameObject.SetActive(true);
            StartCoroutine(CountdownRoutine((int)_soShieldData.CooldownCharge));

            yield return new WaitForSeconds(_soShieldData.CooldownCharge);

            _cooldownButton.gameObject.SetActive(false);

            yield break;

        }

        public IEnumerator CountdownRoutine(int time)
        {
            _cooldownButton.GetComponent<ClickButtonCooldown>().TimeCooldown = time;

            while (time > 0)
            {
                //_cooldownButton.GetComponentInChildren<TextMeshPro>().text = time.ToString();
                yield return new WaitForSeconds(1f);
                time--;
            }
        }

    }
}