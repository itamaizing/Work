using System;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerComponents : MonoBehaviour
{
    [SerializeField] private Character _character;
    [SerializeField] private SelectedCircle CircleSelect;
    [SerializeField] private MinimapMarker MarkersSelect;
    [SerializeField] private FillAmountOverTime _castLine;
    [SerializeField] private Image _castSkillIcon;
    [SerializeField] private Image _castTargetIcon;
    [SerializeField] private TMP_Text _castSkillName;
    [SerializeField] private TMP_Text _castTime;
    [SerializeField] private SkillRenderer skillRenderer;

    [SerializeField] private DamageTracker _damageTracker;
    [SerializeField] private Canvas _UICanvas;

    public Transform DamageSpawn;
    public Transform RegenSpawn;
    public PopupTextPrefab PopupText;

    private Skill _activeCastSkill;
    
    private Color _shieldColor = Color.blue;
    private Color _physDamageColor = Color.red;
    private Color _regenColor = Color.green;

    private float popupSpawnDelay = 0.2f;
    private float _fixDuration = 0.1f;

    private string miss = "Промах";

    private Queue<PopupRequest> popupQueue = new Queue<PopupRequest>();
    private bool _isChannelingBar;
    private bool isProcessingQueue = false;

    public SelectedCircle CircleSelect1 { get => CircleSelect; set => CircleSelect = value; }
    public SkillRenderer Renderer { get => skillRenderer; set => skillRenderer = value; }
    
    private Dictionary<Skill, Action<float, uint>> _castDeleyHandlers = new();
    private Dictionary<Skill, Action> _castDeleyEndHandlers = new();
    private Dictionary<Skill, Action<float, uint>> _castStreamHandlers = new();
    private Dictionary<Skill, Action> _castStreamEndHandlers = new();

    private void Awake()
    {
        _damageTracker = _character.DamageTracker;
        _UICanvas.worldCamera = Camera.main;
    }

    private void OnEnable()
    {
        _character.Health.DamageTaken += OnDamageTaken;
        _character.Health.ShieldDamageTaken += OnShieldDamageTaken;
        _character.Health.OnShieldAdd += OnShieldAdded;
        _character.Health.HealTaked += OnHealTaked;

        foreach (var ability in _character.Abilities.Abilities)
        {
            Action<float, uint> onDeley = (time, targetNetId) => ShowCastBar(time, ability, targetNetId, isChanneling: false);
            Action onDeleyEnd = () => StopCastBar(ability, isChanneling: false);
            Action<float, uint> onStream = (time, targetNetId) => ShowCastBar(time, ability, targetNetId, isChanneling: true);
            Action onStreamEnd = () => StopCastBar(ability, isChanneling: true);

            _castDeleyHandlers[ability] = onDeley;
            _castDeleyEndHandlers[ability] = onDeleyEnd;
            _castStreamHandlers[ability] = onStream;
            _castStreamEndHandlers[ability] = onStreamEnd;

            ability.WorldPreparingStarted += onDeley;
            ability.PreparingEnded += onDeleyEnd;
            ability.Canceled += onDeleyEnd;

            ability.WorldChannelingStarted += onStream;
            ability.ChannelingEnded += onStreamEnd;
            ability.Canceled += onStreamEnd;
        }
    }
    
    private void OnDisable()
    {
        _character.Health.DamageTaken -= OnDamageTaken;
        _character.Health.ShieldDamageTaken -= OnShieldDamageTaken;
        _character.Health.OnShieldAdd -= OnShieldAdded;
        _character.Health.HealTaked -= OnHealTaked;

        foreach (var ability in _character.Abilities.Abilities)
        {
            if (_castDeleyHandlers.TryGetValue(ability, out var onDeley))
                ability.WorldPreparingStarted -= onDeley;
            if (_castDeleyEndHandlers.TryGetValue(ability, out var onDeleyEnd))
            {
                ability.PreparingEnded -= onDeleyEnd;
                ability.Canceled -= onDeleyEnd;
            }
            if (_castStreamHandlers.TryGetValue(ability, out var onStream))
                ability.WorldChannelingStarted -= onStream;
            if (_castStreamEndHandlers.TryGetValue(ability, out var onStreamEnd))
            {
                ability.ChannelingEnded -= onStreamEnd;
                ability.Canceled -= onStreamEnd;
            }
        }

        _castDeleyHandlers.Clear();
        _castDeleyEndHandlers.Clear();
        _castStreamHandlers.Clear();
        _castStreamEndHandlers.Clear();

        HideCastBar();
    }

    public void ChangeSelection(bool isSelect)
    {
        CircleSelect.IsActive = isSelect;
        MarkersSelect.IsActive = isSelect;
    }

    public void ShowPopupValue(float value, Color startColor, Color endColor)
    {
        int intValue = value > 0 ? Mathf.CeilToInt(value) : Mathf.FloorToInt(value);
        if (intValue == 0 && value != 0)
            intValue = value > 0 ? 1 : -1;

        string text = (intValue > 0 ? "+" : "") + intValue;
        popupQueue.Enqueue(new PopupRequest(text, startColor, endColor, DamageSpawn));
        TryStartQueueProcessing();
    }

    public void Miss()
    {
        if (_character.TryGetComponent<UIPlayerComponents>(out var ui))
        {
            ui.ShowPopupText(miss, Color.gray, Color.gray);
        }
    }

    public void ShowPopupText(string text, Color startColor, Color endColor)
    {
        popupQueue.Enqueue(new PopupRequest(text, startColor, endColor, DamageSpawn));
        TryStartQueueProcessing();
    }

    public void ShowPopupValueRegen(float value, Color startColor, Color endColor)
    {
        int intValue = value > 0 ? Mathf.CeilToInt(value) : Mathf.FloorToInt(value);
        if (intValue == 0 && value != 0)
            intValue = value > 0 ? 1 : -1;

        string text = (intValue > 0 ? "+" : "-") + intValue;
        popupQueue.Enqueue(new PopupRequest(text, startColor, endColor, RegenSpawn));
        TryStartQueueProcessing();
    }

    public void ShowPopupTextRegen(string text, Color startColor, Color endColor)
    {
        popupQueue.Enqueue(new PopupRequest(text, startColor, endColor, RegenSpawn));
        TryStartQueueProcessing();
    }

    private void TryStartQueueProcessing()
    {
        if (!isProcessingQueue)
            StartCoroutine(ProcessPopupQueue());
    }

    private IEnumerator ProcessPopupQueue()
    {
        isProcessingQueue = true;

        while (popupQueue.Count > 0)
        {
            var popupData = popupQueue.Dequeue();
            SpawnPopup(popupData);

            yield return new WaitForSeconds(popupSpawnDelay);
        }

        isProcessingQueue = false;
    }

    private void SpawnPopup(PopupRequest request)
    {
        var popup = Instantiate(PopupText, request.SpawnPoint.position, Quaternion.identity, transform);
        popup.PopupText.text = request.Text;
        popup.StartColor = request.StartColor;
        popup.EndColor = request.EndColor;
    }

    private void OnHealTaked(float healValue, Skill skill, string sourceName)
    {
        ShowPopupValueRegen(healValue, _regenColor, _regenColor);
    }

    private void OnDamageTaken(Damage damage, Skill skill)
    {
        if (damage.Value <= 0) return;
        ShowPopupValue(-damage.Value, _physDamageColor, _physDamageColor);
    }

    private void OnShieldDamageTaken(float damageTaken, DamageType damageType, Skill skill)
    {
        ShowPopupValue(-damageTaken, _shieldColor, _shieldColor);
    }

    private void OnShieldAdded(float shieldValue)
    {
        ShowPopupValue(shieldValue, _shieldColor, _shieldColor);
    }


    
    private bool ShouldShow(CastBarObservers flags)
    {
        if (Character.Local == null) return false;

        var relation = Character.Local.RelationTo(_character);
        return relation switch
        {
            CharacterRelation.Self   => flags.HasFlag(CastBarObservers.Self),
            CharacterRelation.Ally   => flags.HasFlag(CastBarObservers.Allies),
            CharacterRelation.Enemy  => flags.HasFlag(CastBarObservers.Enemies),
            _ => false
        };
    }
    
    private void OnChannelingRollback(float rollbackAmount)
    {
        if (_castLine != null)
            _castLine.SkipForward(rollbackAmount);
    }
    
    private void OnPreparingRollback(float rollbackAmount)
    {
        if (_castLine != null)
            _castLine.Rollback(rollbackAmount);
    }

    private void ShowCastBar(float duration, Skill skill, uint targetNetId, bool isChanneling)
    {
        if (_castLine == null || duration <= 0f) return;

        var visibility = isChanneling ? skill.Renderer.CastBarVisibility : skill.Renderer.PrepareBarVisibility;

        if (!ShouldShow(visibility)) return;

        HideCastBar();
        _activeCastSkill = skill;
        _isChannelingBar = isChanneling;

        _castLine.SetColor(skill.Info.AbilityForm switch 
            { AbilityForm.Magic => Color.blue, AbilityForm.Physical => Color.red, _ => Color.white });

        if (_castSkillName != null)
            _castSkillName.text = skill.Name;

        SetCastIcon(_castSkillIcon, skill.Icon);

        Character target = null;
        if (targetNetId != 0 && NetworkClient.spawned.TryGetValue(targetNetId, out var identity))
            target = identity.GetComponent<Character>();

        Sprite portrait = target != null && target.Data != null ? target.Data.Icon : null;
        SetCastIcon(_castTargetIcon, portrait);

        if (isChanneling)
        {
            skill.ChannelingProgressApplied += OnChannelingRollback;
            _castLine.StartFill(duration, 1f, 0f, addTime: false);
        }
        else
        {
            skill.PreparingRolledBack += OnPreparingRollback;
            _castLine.StartFill(duration, 0f, 1f, addTime: false);
        }

        UpdateCastTime(0f, duration);
    }

    private static void SetCastIcon(Image image, Sprite sprite)
    {
        if (image == null) return;

        image.sprite = sprite;
        image.gameObject.SetActive(sprite != null);
    }

    private void StopCastBar(Skill skill, bool isChanneling)
    {
        if (_activeCastSkill == skill && _isChannelingBar == isChanneling)
            HideCastBar();
    }

    private void HideCastBar()
    {
        if (_activeCastSkill != null)
        {
            _activeCastSkill.PreparingRolledBack -= OnPreparingRollback;
            _activeCastSkill.ChannelingProgressApplied -= OnChannelingRollback;
        }

        _activeCastSkill = null;

        if (_castLine != null)
            _castLine.Stop();
    }

    private void LateUpdate()
    {
        if (_castTime == null || _castLine == null || !_castLine.gameObject.activeInHierarchy)
            return;

        UpdateCastTime(_castLine.ElapsedTime, _castLine.Duration);
    }

    private void UpdateCastTime(float elapsed, float duration)
    {
        if (_castTime != null)
            _castTime.text = FormattableString.Invariant($"{elapsed:0.0}/{duration:0.0} sec");
    }

    private void OnHealTracked(Heal heal)
    {
        ShowPopupValueRegen(heal.Value, _regenColor, _regenColor);
    }

    private struct PopupRequest
    {
        public string Text;
        public Color StartColor;
        public Color EndColor;
        public Transform SpawnPoint;

        public PopupRequest(string text, Color startColor, Color endColor, Transform spawnPoint)
        {
            Text = text;
            StartColor = startColor;
            EndColor = endColor;
            SpawnPoint = spawnPoint;
        }
    }
}