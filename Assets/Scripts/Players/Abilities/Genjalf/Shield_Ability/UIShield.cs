using TMPro;
using UnityEngine;

namespace Players.Abilities.Genjalf.Shield_Ability
{
    public class UIShield : MonoBehaviour
    {
        [SerializeField] private GameObject _panelCharges;
        [SerializeField] private TextMeshProUGUI _currentShieldChargeText;
        [SerializeField] private TextMeshProUGUI _resetTimeCharge;


        public void SetTextCharge(int charge)
        {
            //_currentShieldChargeText.text = "= " + charge;
            _currentShieldChargeText.text = charge.ToString();
        }
        
        public void SetTextColor(Color color)
        {
            _currentShieldChargeText.color = color;
        }

        public void SetTextResetTime(float resetTime)
        {
            _resetTimeCharge.text = "" + resetTime.ToString("F0");
        }

        public void SetterHealthUI(float value, float currentHealth)
        {
            //gameObject.transform.parent.GetComponent<HealthPlayer>()._currentHealth -= value;
            gameObject.transform.parent.GetComponent<HealthPlayer>().UpdateHealthBar();
            //gameObject.transform.parent.GetComponent<HealthPlayer>().HealthBarText.text =
            //gameObject.transform.parent.GetComponent<HealthPlayer>().Health.ToString("F0");
            //currentHealth = gameObject.transform.parent.GetComponent<HealthPlayer>()._currentHealth;
        }

        public void ActivePanelCharges()
        {
            _panelCharges.SetActive(true);
        }
    }
}