using TMPro;
using UnityEngine;
using GameDescriptions;

public class AbilityNameBox : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _descriptionWithNumbers;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private TextMeshProUGUI _textState;

    public static readonly string ColorState = "<color=#FFFF00>";
    public static readonly string ColorOpen = "<color=#53935E>";
    public static readonly string ColorEnd = "</color>";

    public void Show(Skill skill)
    {
        _name.text = skill.Name;
        _text.text = DescriptionTable.ToText(SkillDescription.Build(skill, false));
        _descriptionWithNumbers.text = SkillDescription.GetStats(skill);
        if (_textState != null) _textState.text = string.Empty;
    }
}
