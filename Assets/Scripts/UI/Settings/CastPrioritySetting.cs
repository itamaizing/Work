using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CastPriority
{
    Click,
    SelectedTarget,
}

[RequireComponent(typeof(Toggle))]
public sealed class CastPrioritySetting : MonoBehaviour
{
    private const string PreferenceKey = "CastPriority";
    [SerializeField] private TMP_Text _label;
    private Toggle _toggle;

    public static CastPriority Priority => (CastPriority)PlayerPrefs.GetInt(PreferenceKey);

    private void OnEnable()
    {
        _toggle = GetComponent<Toggle>();
        _toggle.SetIsOnWithoutNotify(Priority == CastPriority.SelectedTarget);
        UpdateLabel(_toggle.isOn);
        _toggle.onValueChanged.AddListener(Save);
    }

    private void OnDisable() => _toggle.onValueChanged.RemoveListener(Save);

    private void Save(bool selectedTarget)
    {
        PlayerPrefs.SetInt(PreferenceKey, (int)(selectedTarget ? CastPriority.SelectedTarget : CastPriority.Click));
        PlayerPrefs.Save();
        UpdateLabel(selectedTarget);
    }

    private void UpdateLabel(bool selectedTarget) => _label.text = selectedTarget ? "Приоритет каста — выбранная цель" : "Приоритет каста — клик";
}
