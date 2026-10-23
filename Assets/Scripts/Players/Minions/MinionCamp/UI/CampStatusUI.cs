using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CampStatusUI : MonoBehaviour
{
    [SerializeField] private MinionCamp _camp;
    [SerializeField] private bool _faceCamera = true;

    [Header("Общее ХП")]
    [SerializeField] private GameObject _hpRoot;
    [SerializeField] private Image _hpFill;
    [SerializeField] private TMP_Text _hpText;

    [Header("Таймер лагеря целиком (первое появление / респавн волны)")]
    [SerializeField] private CampRespawnView _campTimerView;

    [Header("Таймеры возрождения рядовых (захваченный лагерь)")]
    [SerializeField] private CampRespawnView _creepViewPrefab;
    [SerializeField] private Transform _creepViewsRoot;

    private readonly Dictionary<int, CampRespawnView> _views = new();
    private readonly HashSet<int> _seen = new();
    private readonly List<int> _stale = new();

    private Camera _camera;
    private int _lastHp = -1;
    private int _lastMaxHp = -1;

    private void Awake()
    {
        if (_camp == null) _camp = GetComponentInParent<MinionCamp>();
    }

    private void Update()
    {
        if (_camp == null || !NetworkClient.active) return;

        UpdateHp();
        UpdateCampTimer();
        UpdateCreepTimers();
    }

    private void LateUpdate()
    {
        if (!_faceCamera) return;

        if (_camera == null) _camera = Camera.main;
        if (_camera != null) transform.forward = _camera.transform.forward;
    }

    private void UpdateHp()
    {
        bool visible = _camp.HpVisible && _camp.HpMax > 0f;
        if (_hpRoot != null && _hpRoot.activeSelf != visible) _hpRoot.SetActive(visible);
        if (!visible) return;

        if (_hpFill != null)
            _hpFill.fillAmount = Mathf.Clamp01(_camp.HpCurrent / _camp.HpMax);

        if (_hpText == null) return;

        int current = Mathf.CeilToInt(_camp.HpCurrent);
        int max = Mathf.CeilToInt(_camp.HpMax);
        if (current == _lastHp && max == _lastMaxHp) return;

        _lastHp = current;
        _lastMaxHp = max;
        _hpText.text = $"{current} / {max}";
    }

    private void UpdateCampTimer()
    {
        if (_campTimerView == null) return;

        if (_camp.TryGetCampRespawn(out float remaining, out float duration, out Sprite icon))
            _campTimerView.Show(icon, remaining, duration);
        else
            _campTimerView.Hide();
    }

    private void UpdateCreepTimers()
    {
        if (_creepViewPrefab == null || _creepViewsRoot == null) return;

        double now = NetworkTime.time;
        var respawns = _camp.CreepRespawns;

        _seen.Clear();
        for (int i = 0; i < respawns.Count; i++)
        {
            var info = respawns[i];
            _seen.Add(info.SlotId);

            if (!_views.TryGetValue(info.SlotId, out var view))
            {
                view = Instantiate(_creepViewPrefab, _creepViewsRoot);
                view.SetIcon(_camp.GetCreepIcon(info));
                _views[info.SlotId] = view;
            }

            view.SetProgress(Mathf.Max(0f, (float)(info.ReadyAt - now)), info.Duration);
        }

        _stale.Clear();
        foreach (var pair in _views)
            if (!_seen.Contains(pair.Key)) _stale.Add(pair.Key);

        foreach (int id in _stale)
        {
            if (_views[id] != null) Destroy(_views[id].gameObject);
            _views.Remove(id);
        }
    }
}