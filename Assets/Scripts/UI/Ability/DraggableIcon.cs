using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _image;

    [SerializeField] private FillAmountOverTime _cooldown;
    [SerializeField] private TextMeshProUGUI _chargeCounter;
    [SerializeField] private Blink _blinkBoxFrame;
    [SerializeField] private AutoCastParticles _autoCastEffectPrefab;

    private Transform _patentAfterDrag;
    private Skill _skill;
    private bool _selected;
    private AutoCastParticles _autoCastEffect;

    public Transform PatentAfterDrag { get => _patentAfterDrag; set => _patentAfterDrag = value; }
    public Skill Skill { get => _skill; set => _skill = value; }
    public bool Selected { get => _selected; set => _selected = value; }

    public event Action BeginDrag;
    public event Action EndDrag;
    public event Action<DraggableIcon> PointerEnter;
    public event Action<DraggableIcon> PointerExit;

    public void Init(Skill skill, Transform parent)
    {
        _skill = skill;
        _image.sprite = _skill.Icon;
        PatentAfterDrag = parent;

        if (skill is AutoAttackSkill)
        {
            _autoCastEffect = Instantiate(_autoCastEffectPrefab, transform);
        }

        SubscribingSkillOnEvents(_skill);
    }

    private void OnDestroy()
    {
        UnsubscribingSkillOnEvents(_skill);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        PatentAfterDrag = transform.parent;
        PatentAfterDrag.GetComponent<SkillIcon>().CurrentIcon = null;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        _image.raycastTarget = false;

        BeginDrag?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(PatentAfterDrag);
        transform.SetAsFirstSibling();
        _image.raycastTarget = true;
        PatentAfterDrag.GetComponent<SkillIcon>().CurrentIcon = this;

        EndDrag?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        InputHandler.OnSwitchAutoMode += OnClickWithCtrl;

        PointerEnter?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InputHandler.OnSwitchAutoMode -= OnClickWithCtrl;

        PointerExit?.Invoke(this);
    }

    private void OnStartAutoAttack()
    {
        _autoCastEffect.gameObject.SetActive(true);
        _autoCastEffect.Play();
        Debug.LogWarning("OnStartAuto!");
    }

    private void OnEndAutoAttack()
    {
        _autoCastEffect.gameObject.SetActive(false);
        Debug.LogWarning("OnEndAuto!!!!!!!!!!!!!!!");
    }

    private void SubscribingSkillOnEvents(Skill ability)
    {
        //ability.CastStreamStarted += OnStartStreaming;
        //ability.Canceled += OnStopStreaming;

        //ability.CastDeleyStarted += OnStartCastDeley;
        //ability.Canceled += OnStopCastDeley;

        ability.CooldownStarted += OnStartCooldown;
        ability.CurrentChargeChanged += OnCurrentChargeText;

        ability.CastStarted += OnCastStarted;
        ability.CastEnded += OnCastEnded;
        ability.Canceled += OnCastEnded;

        ability.CooldownEnded += OnStopCooldown;

        if (ability is AutoAttackSkill autoAttackSkill)
        {
            autoAttackSkill.Canceled += OnEndAutoAttack;
            // autoAttackSkill.CastPaused += OnEndAutoAttack;
            autoAttackSkill.CastStarted += OnStartAutoAttack;
            // autoAttackSkill.CastContinued += OnStartAutoAttack;
            //autoAttackSkill.AutoCastEnded +=
        }
    }

    private void UnsubscribingSkillOnEvents(Skill ability)
    {
        //ability.CastStreamStarted -= OnStartStreaming;
        //ability.Canceled -= OnStopStreaming;

        //ability.CastDeleyStarted -= OnStartCastDeley;
        //ability.Canceled -= OnStopCastDeley;

        ability.CooldownStarted -= OnStartCooldown;
        ability.CurrentChargeChanged -= OnCurrentChargeText;

        ability.CastStarted -= OnCastStarted;
        ability.CastEnded -= OnCastEnded;
        ability.Canceled -= OnCastEnded;

        ability.CooldownEnded -= OnStopCooldown;

        if (ability is AutoAttackSkill autoAttackSkill)
        {
            autoAttackSkill.Canceled -= OnEndAutoAttack;
            // autoAttackSkill.CastPaused -= OnEndAutoAttack;
            autoAttackSkill.CastStarted -= OnStartAutoAttack;
            // autoAttackSkill.CastContinued -= OnStartAutoAttack;
            //autoAttackSkill.AutoCastEnded -=
        }
    }

    private void OnStopCooldown()
    {
        _cooldown.Stop();
    }

    private void OnClickWithCtrl()
    {
        if (Skill is AutoAttackSkill autuAttackSkill)
        {
            autuAttackSkill.SwitchAutoMode();
            Debug.Log("AA mode - " + autuAttackSkill.IsAutoattackMode);
        }
    }

    private void OnCastStarted()
    {
        _blinkBoxFrame.gameObject.SetActive(true);
        _blinkBoxFrame.StartBlink(0.5f);
    }

    private void OnCastEnded()
    {
        _blinkBoxFrame.StopBlink();
        _blinkBoxFrame.gameObject.SetActive(false);
    }

    private void OnCurrentChargeText(int value)
    {
        if (value > 0)
            _chargeCounter.color = Color.green;
        else
            _chargeCounter.color = Color.red;

        _chargeCounter.text = value.ToString();
    }

    private void OnStartCooldown(float dutarion)
    {
        _cooldown.gameObject.SetActive(true);
        _cooldown.StartFill(dutarion, 1, 0, false);
    }
}