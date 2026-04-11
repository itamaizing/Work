using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _image;

    [SerializeField] private ChargeCDUI _chargeCD;
    [SerializeField] private FillAmountOverTime _cooldown;
    [SerializeField] private TMP_Text _cooldownNum;
    [SerializeField] private TextMeshProUGUI _chargeCounter;
    [SerializeField] private Blink _blinkBoxFrame;
    [SerializeField] private AutoCastParticles _autoCastEffect;
    [SerializeField] private ParticleSystem _boostEffect;

    private Transform _patentAfterDrag;
    private Skill _skill;
    private bool _selected;
    private Camera _camera;
    private float _distance;
    private Coroutine _cooldownCoroutine;
    private float _pendingCooldown = -1f;
    private bool _isMenu = false;

    public Transform ParentAfterDrag { get => _patentAfterDrag; set => _patentAfterDrag = value; }
    public Skill Skill { get => _skill; set => _skill = value; }
    public bool Selected { get => _selected; set => _selected = value; }

    public event Action BeginDrag;
    public event Action EndDrag;
    public event Action<DraggableIcon> PointerEnter;
    public event Action<DraggableIcon> PointerExit;

    private void OnEnable()
    {
        if (_pendingCooldown > 0f)
        {
            OnStartCooldown(_pendingCooldown);
            _pendingCooldown = -1f;
        }
    }

    public void Init(Skill skill, Transform parent, Camera camera, float distance, bool isMenu = false)
    {
        _skill = skill;
        _isMenu = isMenu;
        _image.sprite = _skill.Icon;
        ParentAfterDrag = parent;
        _camera = camera;
        _distance = distance;
        _skill.LinkedChargeCDUI = _chargeCD;

        _skill.OnSkillStateChanged += UpdateIconState;
        //_skill.ChargeCooldownEnded += OnChargeCooldownEnded;
        _skill.Charges.OnRechargeEnd += OnChargeCooldownEnded; //new

        UpdateIconState(_skill.Disactive);

        if (_skill.Charges.UsesCharges == true)
        {
            _chargeCounter.gameObject.SetActive(true);
            OnCurrentChargeChanged(_skill.Chargers);
        }

        SubscribingSkillOnEvents(_skill);

        UpdateAllInfo();
    }

    private void OnDestroy()
    {
        UnsubscribingSkillOnEvents(_skill);
        _skill.OnSkillStateChanged -= UpdateIconState;
        //_skill.ChargeCooldownEnded -= OnChargeCooldownEnded;
        _skill.Charges.OnRechargeEnd -= OnChargeCooldownEnded; //new
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ParentAfterDrag = transform.parent;
        ParentAfterDrag.GetComponent<SkillIcon>().CurrentIcon = null;

        if (!_isMenu)
        {
            transform.SetParent(transform.root);
            transform.SetAsLastSibling();
        }
        
        _image.raycastTarget = false;

        BeginDrag?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 mousePosition = Input.mousePosition;
        Vector3 screenPos = new Vector3(mousePosition.x, mousePosition.y, _distance);
        Vector3 worldPos = _camera.ScreenToWorldPoint(screenPos);

        if (_isMenu)
            worldPos = screenPos;

        transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(ParentAfterDrag);
        transform.SetAsFirstSibling();

        if(_isMenu)
            transform.position = ParentAfterDrag.position;

        _image.raycastTarget = true;
        ParentAfterDrag.GetComponent<SkillIcon>().CurrentIcon = this;

        EndDrag?.Invoke();
    }

    public void UpdatePosition(Transform parent)
    {
        ParentAfterDrag = transform.parent;
        ParentAfterDrag.GetComponent<SkillIcon>().CurrentIcon = null;

        ParentAfterDrag = parent;
        transform.position = ParentAfterDrag.position;
        transform.SetParent(ParentAfterDrag);
        transform.SetAsFirstSibling();

        ParentAfterDrag.GetComponent<SkillIcon>().CurrentIcon = this;
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

    public void UpdateIconState(bool disactive)
    {
        _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, disactive ? 0.5f : 1f);
    }

    private void OnChargeCooldownEnded(int index)
    {
        _chargeCD.RemoveChargeCD(index);
    }

    private void UpdateAllInfo()
    {
        OnStartCooldown(0);
        OnAutoModeChanged(_skill.IsAutoMode);
    }

    private void OnAutoModeChanged(bool value)
    {
        if (value)
            OnStartAutoAttack();
        else
            OnEndAutoAttack();
    }

    private void OnStartAutoAttack()
    {
        _autoCastEffect.Play();
    }

    private void OnEndAutoAttack()
    {
        _autoCastEffect.Stop();
    }

    private void SubscribingSkillOnEvents(Skill ability)
    {
        //ability.CooldownStarted += OnStartCooldown;
        //ability.CurrentChargeChanged += OnCurrentChargeChanged;
        //ability.ChargeStartCooldown += OnChargeStartCooldown;

        ability.CastStarted += OnCastStarted;
        ability.CastEnded += OnCastEnded;
        ability.Canceled += OnCastEnded;

        //ability.CooldownEnded += OnStopCooldown;

        ability.AutoModeChanged += OnAutoModeChanged;

        ability.BoostEnabled += OnBoostEnabled;
        ability.BoostDisabled += OnBoostDisabled;

        //new
        ability.Cooldown.OnStart += OnStartCooldown;
        ability.Cooldown.OnEnd += OnStopCooldown;
        ability.Charges.OnRechargeStart += OnChargeStartCooldown;
        ability.Charges.OnCurrentChange += OnCurrentChargeChanged;
    }

    private void UnsubscribingSkillOnEvents(Skill ability)
    {
        //ability.CooldownStarted -= OnStartCooldown;
        //ability.CurrentChargeChanged -= OnCurrentChargeChanged;
        //ability.ChargeStartCooldown -= OnChargeStartCooldown;

        ability.CastStarted -= OnCastStarted;
        ability.CastEnded -= OnCastEnded;
        ability.Canceled -= OnCastEnded;

        //ability.CooldownEnded -= OnStopCooldown;

        ability.AutoModeChanged -= OnAutoModeChanged;

        ability.BoostEnabled -= OnBoostEnabled;
        ability.BoostDisabled -= OnBoostDisabled;

        //new
        ability.Cooldown.OnStart -= OnStartCooldown;
        ability.Cooldown.OnEnd -= OnStopCooldown;
        ability.Charges.OnRechargeStart -= OnChargeStartCooldown;
        ability.Charges.OnCurrentChange -= OnCurrentChargeChanged;
    }

    private void OnBoostDisabled()
    {
        _boostEffect.gameObject.SetActive(false);
    }

    private void OnBoostEnabled()
    {
        _boostEffect?.gameObject.SetActive(true);
    }

    private void OnClickWithCtrl()
    {
        if (Skill.Info.AutoAttack != AutoAttack.autoAttack) return;

        Skill.IsAutoMode = !Skill.IsAutoMode;
        Debug.Log("AA mode - " + Skill.IsAutoMode);

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

    private void OnCurrentChargeChanged(int value)
    {
        if (value > 0)
        {
            _chargeCounter.gameObject.SetActive(true);
            _chargeCounter.color = Color.green;
        }
        else
        {
            _chargeCounter.gameObject.SetActive(false);
            _chargeCounter.color = Color.red;
        }
        _chargeCounter.text = value.ToString();
    }

    private void OnStartCooldown(float duration)
    {
        if (!gameObject.activeInHierarchy)
        {
            _pendingCooldown = duration;
            return;
        }

        if (_cooldownCoroutine != null) StopCoroutine(_cooldownCoroutine);

        _cooldown.gameObject.SetActive(true);

        CheckingForCurrentRecharge(duration);

        _cooldownNum.color = (_skill is IPassiveSkill) ? Color.green : Color.red;

        _cooldownCoroutine = StartCoroutine(CooldownCounterJob(duration));
    }

    private void OnStopCooldown()
    {
        _cooldown.Stop();

        if (_cooldownCoroutine != null)
            StopCoroutine(_cooldownCoroutine);

        _cooldownNum.gameObject.SetActive(false);
        _pendingCooldown = -1f;
    }

    private void OnChargeStartCooldown(float value)
    {
        _chargeCD.AddChargeCD(value);
    }

    private void CheckingForCurrentRecharge(float duration)
    {
        float time;
        float startValue = 1f;

        bool isActiveSkill = _skill != null && _skill.Cooldown.RemainingTime > 0.01f;

        if (isActiveSkill)
        {
            time = startValue - duration / _skill.Cooldown.CooldownTime;
            _cooldown.StartFill(duration, startValue - time, 0, false);
        }

        else _cooldown.StartFill(duration, 1, 0, false);
    }

    private IEnumerator CooldownCounterJob(float dutarion)
    {
        float time = dutarion;
        while (dutarion > 0)
        {
            if (_skill.Charges.UsesCharges == false || _skill.Chargers <= 0)
                _cooldownNum.gameObject.SetActive(true);
            else
                _cooldownNum.gameObject.SetActive(false);

            _cooldownNum.text = dutarion.ToString("0");
            yield return null;
            dutarion -= Time.deltaTime;
        }
        _cooldownCoroutine = null;
    }
}