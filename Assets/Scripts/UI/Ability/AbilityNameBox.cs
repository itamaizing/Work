using TMPro;
using UnityEngine;
using GameDescriptions;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public enum AbilityDescriptionIcon
{
    Targeting, Targets, Cooldown, Cost, Range, Area, Preparing, Channeling, Damage, School, Charges, Counter,
    Health, Mana, Energy, Rune, Psionic, AttackingPsi, CooldownEnergy
}

public class AbilityNameBox : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _descriptionWithNumbers;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private TextMeshProUGUI _textState;
    [SerializeField] private Image _separatorPrefab;
    [SerializeField, Min(1f)] private float _separatorThickness = 2f;
    [SerializeField, Min(0.5f)] private float _iconSizeInEm = 1f;
    [SerializeField] private List<IconBinding> _icons = new();

    [Serializable]
    public sealed class IconBinding
    {
        public AbilityDescriptionIcon Kind;
        public Sprite Sprite;
    }

    private RectTransform _content;

    public static readonly string ColorState = "<color=#FFFF00>";
    public static readonly string ColorOpen = "<color=#53935E>";
    public static readonly string ColorEnd = "</color>";

    public void Show(Skill skill)
    {
        EnsureContent();
        ClearContent();
        _name.text = skill != null ? skill.Name : string.Empty;
        if (skill == null) return;

        var targets = CreateRow("Targets");
        AddIconValue(AbilityDescriptionIcon.Targeting, SkillDescription.GetClickTargetsText(skill), targets);
        AddIconValue(AbilityDescriptionIcon.Targets, SkillDescription.GetTargetFactionsText(skill), targets);

        var stats = CreateRow("Parameters");
        var parameters = SkillDescription.GetParameters(skill);
        
        foreach (var parameter in parameters)
        {
            if (parameter.Kind != DescriptionParameterKind.Cost) continue;
            var costs = new GameObject("Costs", typeof(RectTransform), typeof(VerticalLayoutGroup));
            costs.transform.SetParent(stats, false);
            var layout = costs.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            if (parameter.Costs != null && parameter.Costs.Count > 0)
            {
                foreach (var cost in parameter.Costs)
                    AddIconValue(ResourceIcon(cost.Resource), cost.Value, costs.transform);
            }
            else AddIconValue(AbilityDescriptionIcon.Cost, parameter.CompactValue, costs.transform);
        }
        foreach (var parameter in parameters)
            if (parameter.Kind != DescriptionParameterKind.Cost)
                AddIconValue(ParameterIcon(parameter.Kind), parameter.CompactValue, stats);

        var rows = SkillDescription.Build(skill, false);
        
        if (rows.Count > 0 && !rows[0].IsLineBreak) AddSeparator(DescriptionRow.LineBreak());
        foreach (var row in rows)
        {
            if (row.IsLineBreak) AddSeparator(row);
            else AddText(row.Text, _text, _content);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }

    private void EnsureContent()
    {
        if (_content != null) return;
        var content = new GameObject("DescriptionContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
        _content = (RectTransform)content.transform;
        _content.SetParent(_text.transform.parent, false);
        _content.SetSiblingIndex(_text.transform.GetSiblingIndex());
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.sizeDelta = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 4f;
        Vector4 margin = _name.margin;
        layout.padding = new RectOffset(Mathf.Max(0, Mathf.CeilToInt(margin.x)),
            Mathf.Max(0, Mathf.CeilToInt(margin.z)), 0,
            Mathf.Max(0, Mathf.CeilToInt(margin.w)));

        _text.gameObject.SetActive(false);
        if (_descriptionWithNumbers != null) _descriptionWithNumbers.gameObject.SetActive(false);
        if (_textState != null) _textState.gameObject.SetActive(false);
    }

    private void ClearContent()
    {
        for (int index = _content.childCount - 1; index >= 0; index--)
        {
            GameObject child = _content.GetChild(index).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private void AddText(string text, TextMeshProUGUI template, Transform parent)
    {
        if (string.IsNullOrEmpty(text)) return;
        template = template != null ? template : _text;
        
        var labelObject = new GameObject("DescriptionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = template.font;
        label.fontSharedMaterial = template.fontSharedMaterial;
        label.fontSize = template.fontSize;
        label.fontStyle = template.fontStyle;
        label.color = template.color;
        label.lineSpacing = template.lineSpacing;
        label.characterSpacing = template.characterSpacing;
        label.wordSpacing = template.wordSpacing;
        label.text = text;
        label.margin = Vector4.zero;
        label.richText = true;
        label.raycastTarget = false;
        label.enableAutoSizing = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.enabled = true;
        var fitter = label.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;
        var layout = label.GetComponent<LayoutElement>();
        if (layout == null) layout = label.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = false;
        layout.minWidth = 0f;
        layout.preferredWidth = -1f;
        layout.minHeight = layout.preferredHeight = -1f;
        layout.flexibleWidth = 1f;
        layout.flexibleHeight = 0f;
        label.gameObject.SetActive(true);
    }

    private Transform CreateRow(string name)
    {
        var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(_content, false);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        return row.transform;
    }

    private void AddIconValue(AbilityDescriptionIcon kind, string value, Transform parent)
    {
        if (string.IsNullOrEmpty(value)) return;
        var item = new GameObject(kind.ToString(), typeof(RectTransform), typeof(HorizontalLayoutGroup));
        item.transform.SetParent(parent, false);
        var layout = item.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        Sprite sprite = null;
        foreach (var binding in _icons)
            if (binding != null && binding.Kind == kind && binding.Sprite != null) { sprite = binding.Sprite; break; }
        if (sprite != null)
        {
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            icon.transform.SetParent(item.transform, false);
            var image = icon.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var element = icon.GetComponent<LayoutElement>();
            float size = (_descriptionWithNumbers != null ? _descriptionWithNumbers : _text).fontSize * _iconSizeInEm;
            element.minWidth = element.preferredWidth = size;
            element.minHeight = element.preferredHeight = size;
            element.flexibleWidth = element.flexibleHeight = 0f;
        }
        var template = _descriptionWithNumbers != null ? _descriptionWithNumbers : _text;
        AddText(value, template, item.transform);
        var label = item.GetComponentInChildren<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.TopLeft;
        label.GetComponent<LayoutElement>().flexibleWidth = 0f;
    }

    private static AbilityDescriptionIcon ResourceIcon(ResourceType type) => type switch
    {
        ResourceType.Health => AbilityDescriptionIcon.Health,
        ResourceType.Mana => AbilityDescriptionIcon.Mana,
        ResourceType.Energy => AbilityDescriptionIcon.Energy,
        ResourceType.Rune => AbilityDescriptionIcon.Rune,
        ResourceType.Psionic => AbilityDescriptionIcon.Psionic,
        ResourceType.AttackingPsi => AbilityDescriptionIcon.AttackingPsi,
        ResourceType.CooldownEnergy => AbilityDescriptionIcon.CooldownEnergy,
        _ => AbilityDescriptionIcon.Cost
    };

    private static AbilityDescriptionIcon ParameterIcon(DescriptionParameterKind kind) => kind switch
    {
        DescriptionParameterKind.Cooldown => AbilityDescriptionIcon.Cooldown,
        DescriptionParameterKind.Range => AbilityDescriptionIcon.Range,
        DescriptionParameterKind.Area => AbilityDescriptionIcon.Area,
        DescriptionParameterKind.Preparing => AbilityDescriptionIcon.Preparing,
        DescriptionParameterKind.Channeling => AbilityDescriptionIcon.Channeling,
        DescriptionParameterKind.Damage => AbilityDescriptionIcon.Damage,
        DescriptionParameterKind.School => AbilityDescriptionIcon.School,
        DescriptionParameterKind.Charges => AbilityDescriptionIcon.Charges,
        _ => AbilityDescriptionIcon.Counter
    };

    private void AddSeparator(DescriptionRow row)
    {
        var holder = new GameObject("DescriptionLineBreak", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        holder.transform.SetParent(_content, false);
        var layout = holder.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(Mathf.CeilToInt(row.LeftPadding), Mathf.CeilToInt(row.RightPadding), 4, 4);
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Image line;
        if (_separatorPrefab != null)
        {
            line = Instantiate(_separatorPrefab, holder.transform, false);
            line.raycastTarget = false;
            line.gameObject.SetActive(true);
            var element = line.GetComponent<LayoutElement>();
            if (element == null) element = line.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = false;
            element.minWidth = 0f;
            element.preferredWidth = -1f;
            element.flexibleWidth = 1f;
            element.minHeight = element.preferredHeight = Mathf.Max(1f, _separatorThickness);
            element.flexibleHeight = 0f;
        }
    }
}
