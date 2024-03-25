using System.Collections;
using TMPro;
using UnityEngine;

namespace Players.Abilities.Genjalf.Test_Shield
{
    public class TestShield : MonoBehaviour
    {
        [SerializeField] private SoShieldData _soShieldData;
        [SerializeField] private TextMeshProUGUI _textShield;
        [SerializeField] private TextMeshProUGUI _textShieldCharges;

        private Coroutine _coroutineActiveShield;
        private Coroutine _resetCoroutine;
        private int _currentShieldCharge;
        private bool _isResetCoroutineRunning = false;

        private void Start()
        {
            _currentShieldCharge = _soShieldData.ShieldCharges;
            _textShieldCharges.text = "Charges Shield: " + _currentShieldCharge.ToString();
        }

        private void Update()
        {
            CheckChargeOnStartReset();

            _textShieldCharges.text = "Charges Shield: " + _currentShieldCharge.ToString();

            if (Input.GetKeyDown(KeyCode.Alpha1))
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

            yield return new WaitForSeconds(durationCast);
            _currentShieldCharge--;
            gameObject.GetComponent<SpriteRenderer>().color = Color.cyan;
            _textShield.text = "Shield: " + _soShieldData.AbsorptionAmount.ToString();
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
    }
}