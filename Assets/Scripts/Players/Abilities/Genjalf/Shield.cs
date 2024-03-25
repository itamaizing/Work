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

        private bool _canCast = true;
        private bool _isGlobalCooldown;
        private GameObject _newCastPrefab;
        private bool _isEnabled = false;
        private Coroutine _coroutine;


        private void Update()
        {
            ActivatedAbility();
        }

        private void ActivatedAbility()
        {
            if (_toggleAbility.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Alpha1) &&
                transform.parent.GetComponent<PlayerMove>().IsSelect && _toggleAbility.enabled)
            {
                if (_toggleAbility.isOn)
                {
                    _toggleAbility.isOn = false;
                }
                else
                {
                    _toggleAbility.isOn = true;
                }
            }

            if (_toggleAbility.isOn == true)
            {
                _isEnabled = false;

                _iconAbility.GetComponent<SpriteRenderer>().enabled = true;
                if (_canCast)
                {
                    //_coroutine = StartCoroutine(Cast(1.5f));
                }
                else if (_canCast)
                {
                    //StartDarkBeginning(0);
                }
            }
            else
            {
                _canCast = true;
                _iconAbility.GetComponent<SpriteRenderer>().enabled = false;
            }

            if (_coroutine != null)
            {
                _toggleAbility.enabled = false;
            } 
        }
    }
}