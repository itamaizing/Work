using GlobalEvents;
using UnityEngine;
using UnityEngine.UI;

namespace Players.Abilities.Genjalf.Fireworks_Ability.Version_In_Genjalf_Fireworks
{
    public class ShootFireworks : MonoBehaviour
    {
        [SerializeField] private GameObject _fireWorks;
        [SerializeField] private GameObject _iconAbility;
        [SerializeField] private Toggle _toggleAbility;
        [SerializeField] private GameObject _abilitiesPanel;
        [SerializeField] private GameObject _castPrefab;
        [SerializeField] private GameObject _manaCost;

        private float _currentMana;
        private float _startSpeedPlayer;
        private bool _isGlobalCooldown;


        private void Awake()
        {
            StartFireworksEvent.OnStartFireworksEvent.AddListener(_fireWorks.GetComponent<Fireworks>()
                .StartTimeToEndFireworks);

            StopFireworksEvent.OnStopFireworksEvent.AddListener(DisableFireworks);
        }

        private void Start()
        {
            _startSpeedPlayer = gameObject.transform.parent.GetComponent<PlayerMove>().MoveSpeed;
            _currentMana = gameObject.transform.parent.GetComponent<ManaPlayer>().Mana;
        }

        private void Update()
        {
            ActivatedAbility();
        }

        private void ActivatedAbility()
        {
            if (_toggleAbility.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Alpha2) &&
                transform.parent.GetComponent<PlayerMove>().IsSelect && _toggleAbility.enabled)
            {
                ActivatedFireworks();
            }
        }


        private void ActivatedFireworks()
        {
            gameObject.transform.parent.GetComponent<PlayerMove>().MoveSpeed = 0;

            if (_currentMana > 0)
            {
                if (!_isGlobalCooldown)
                {
                    _abilitiesPanel.GetComponent<GlobalCooldown>().StartGlobalCooldown();
                    _isGlobalCooldown = true;
                }

                _fireWorks.SetActive(true);
                StartFireworksEvent.SendStartFireworksEvent();

                _iconAbility.GetComponent<SpriteRenderer>().enabled = true;
                _manaCost.SetActive(true);
                _manaCost.GetComponent<VisualManaCost>().CheckManaCost();
                _manaCost.transform.localScale = new Vector2(2f, _manaCost.gameObject.transform.localScale.y);
            }
        }

        private void DisableFireworks()
        {
            _fireWorks.GetComponent<Fireworks>().StopTimeToEndFireworks();
            _fireWorks.SetActive(false);
            _iconAbility.GetComponent<SpriteRenderer>().enabled = false;
            _manaCost.SetActive(false);
            transform.parent.GetComponent<PlayerMove>().MoveSpeed = _startSpeedPlayer;
            _isGlobalCooldown = false;
        }
    }
}