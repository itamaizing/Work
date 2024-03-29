using System.Collections;
using GlobalEvents;
using UnityEngine;
using UnityEngine.UI;

namespace Players.Abilities.Genjalf.Fireworks_Ability.Version_In_Genjalf_Fireworks
{
    public class ShootFireworks : MonoBehaviour
    {
        [SerializeField] private GameObject _fireWorks;
        [SerializeField] private GameObject _fireWorksPreAblity;
        [SerializeField] private GameObject _iconAbility;
        [SerializeField] private Toggle _toggleAbility;
        [SerializeField] private GameObject _abilitiesPanel;
        [SerializeField] private GameObject _manaCost;

        private float _currentMana;
        private float _startSpeedPlayer;
        private bool _isGlobalCooldown;
        private bool _abilityActivated = false;
        private Coroutine _blinkCoroutine;
        private SpriteRenderer _startIconAbility;


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
            CanselPreGuidingAbility();
            ActivatedAbility();
        }

        private void CanselPreGuidingAbility()
        {
            if (Input.GetMouseButtonDown(1))
            {
                _fireWorksPreAblity.SetActive(false);
                _iconAbility.GetComponent<SpriteRenderer>().enabled = false;
                _abilityActivated = false;
            }
        }

        private void PreGuidingAbility()
        {
            _fireWorksPreAblity.SetActive(true);
            _iconAbility.GetComponent<SpriteRenderer>().enabled = true;
        }

        //Активация наведения способности по нажатию клавиши, при повторном нажатии, вкелючается способность.
        private void ActivatedAbility()
        {
            if (_toggleAbility.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Alpha2) &&
                transform.parent.GetComponent<PlayerMove>().IsSelect && _toggleAbility.enabled)
            {
                if (!_abilityActivated)
                {
                    PreGuidingAbility();
                    if (_currentMana > 0)
                        _abilityActivated = true;
                }
                else
                {
                    ActivatedFireworks();
                }
            }
        }

        //Включаем способность
        private void ActivatedFireworks()
        {
            _fireWorksPreAblity.SetActive(false);
            gameObject.transform.parent.GetComponent<PlayerMove>().MoveSpeed = 0;

            if (_currentMana > 0)
            {
                if (!_isGlobalCooldown)
                {
                    _abilitiesPanel.GetComponent<GlobalCooldown>().StartGlobalCooldown();
                    _isGlobalCooldown = true;
                }

                if (_blinkCoroutine == null)
                {
                    _blinkCoroutine = StartCoroutine(Blink());
                }

                _fireWorks.SetActive(true);
                StartFireworksEvent.SendStartFireworksEvent();

                _manaCost.SetActive(true);
                _manaCost.GetComponent<VisualManaCost>().CheckManaCost();
                _manaCost.transform.localScale = new Vector2(2f, _manaCost.gameObject.transform.localScale.y);
            }
        }

        // Отключаем способность
        private void DisableFireworks()
        {
            _fireWorks.GetComponent<Fireworks>().StopTimeToEndFireworks();
            _fireWorks.SetActive(false);

            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
                _blinkCoroutine = null;
            }

            SetAlphaIconAbility();
            _iconAbility.GetComponent<SpriteRenderer>().enabled = false;


            _manaCost.SetActive(false);
            transform.parent.GetComponent<PlayerMove>().MoveSpeed = _startSpeedPlayer;
            _isGlobalCooldown = false;
            _abilityActivated = false;
        }

        private void SetAlphaIconAbility()
        {
            SpriteRenderer spriteRenderer = _iconAbility.GetComponent<SpriteRenderer>();
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }

        private IEnumerator Blink()
        {
            while (true)
            {
                // Затухание
                for (float t = 0f; t < 1; t += Time.deltaTime)
                {
                    float normalizedTime = t / 1;
                    float alpha = Mathf.Lerp(1f, 0f, normalizedTime);

                    Color newColor = _iconAbility.GetComponent<SpriteRenderer>().color;
                    newColor.a = alpha;
                    _iconAbility.GetComponent<SpriteRenderer>().color = newColor;

                    Color newAutoattackColor = _iconAbility.GetComponent<SpriteRenderer>().color;
                    newAutoattackColor.a = alpha;
                    _iconAbility.GetComponent<SpriteRenderer>().color = newAutoattackColor;

                    yield return null;
                }

                // Появление
                for (float t = 0f; t < 1; t += Time.deltaTime)
                {
                    float normalizedTime = t / 1;
                    float alpha = Mathf.Lerp(0f, 1f, normalizedTime);

                    Color newColor = _iconAbility.GetComponent<SpriteRenderer>().color;
                    newColor.a = alpha;
                    _iconAbility.GetComponent<SpriteRenderer>().color = newColor;

                    Color newAutoattackColor = _iconAbility.GetComponent<SpriteRenderer>().color;
                    newAutoattackColor.a = alpha;
                    _iconAbility.GetComponent<SpriteRenderer>().color = newAutoattackColor;

                    yield return null;
                }
            }
        }
    }
}