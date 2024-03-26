using TMPro;
using UnityEngine;

namespace Players.Abilities.Genjalf
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
        
        public void ActivePanelCharges()
        {
            _panelCharges.SetActive(true);
        }
    }
}