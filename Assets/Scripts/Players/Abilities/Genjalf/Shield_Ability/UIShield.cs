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
            _currentShieldChargeText.text = "= " + charge;
        }

        public void SetTextResetTime(float resetTime)
        {
            _resetTimeCharge.text = "" + resetTime.ToString("F0");
        }

        public void SetterHealthUI(float value, float currentHealth)
        {
            gameObject.transform.parent.GetComponent<HealthPlayer>().Health -= value;
            gameObject.transform.parent.GetComponent<HealthPlayer>().UpdateHealthBar();
            gameObject.transform.parent.GetComponent<HealthPlayer>().HealthBarText.text =
                gameObject.transform.parent.GetComponent<HealthPlayer>().Health.ToString("F0");
            currentHealth = gameObject.transform.parent.GetComponent<HealthPlayer>().Health;
        }

        public void ActivePanelCharges()
        {
            _panelCharges.SetActive(true);
        }
    }
}