using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AbilityNameBox : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _descriptionWithNumbers;
    [SerializeField] private TextMeshProUGUI _text;

    public static readonly string ColorState= "<color=#FFFF00>"; // test: state color
    public static readonly string ColorOpen = "<color=#53935E>";
    public static readonly string ColorEnd = "</color>";

    public void Show(Skill skill)
    {
        _name.text = skill.Name;
        _text.text = $"{skill.Description} \n'{ColorState}{skill.State}{ColorEnd}' - {skill.DescriptionState}";
        _descriptionWithNumbers.text = "";

        if (skill.SkillEnergyCosts.Count > 0)
            _descriptionWithNumbers.text = $"Затрата: {ColorOpen}{skill.SkillEnergyCosts[0].resourceCost} ед. маны{ColorEnd}";
        else
            _descriptionWithNumbers.text = $"Затрата: {ColorOpen}0 ед. маны{ColorEnd}";

        if (skill.ManaCostPerTick.Count > 0)
            _descriptionWithNumbers.text += $" и {ColorOpen}{skill.ManaCostPerTick[0].resourceCost} ед. маны/{skill.ManaCostRate} сек{ColorEnd}";

        if (skill.Damage > 0)
        {
            _descriptionWithNumbers.text += $"\nУрон: {ColorOpen}{skill.Damage}{ColorEnd}";
        }

        WriteTypeDamage(skill);
        WriteTypeAbityForm(skill);

        if (skill.CastDeley > 0)
            _descriptionWithNumbers.text += $"\nПодготовка: {ColorOpen}{skill.CastDeley} сек{ColorEnd}";

        if(skill.CastStreamDuration > 0)
            _descriptionWithNumbers.text += $"\nВыполнение: {ColorOpen}{skill.CastStreamDuration} сек{ColorEnd}";

        if (skill.CooldownTime > 0)
            _descriptionWithNumbers.text += $"\nПерезарядка: {ColorOpen}{skill.CooldownTime} сек{ColorEnd}";

        if (skill.ChargeCooldown > 0)
            _descriptionWithNumbers.text += $"\nКол-во Зарядов: {ColorOpen}{skill.MaxChargers}/{skill.ChargeCooldown} сек{ColorEnd}";

        //if (skill.AdditionalDescription != string.Empty)
        //    _descriptionWithNumbers.text += $"\n{skill.AdditionalDescription}";
    }

    private void WriteTypeAbityForm(Skill skill)
    {
        _descriptionWithNumbers.text += "\n Форма способности:";

        switch (skill.AbilityForm)
        {
            case AbilityForm.Magic:
                _descriptionWithNumbers.text += " магия";
                break;
            case AbilityForm.Physical:
                _descriptionWithNumbers.text += " физика";
                break;
            case AbilityForm.Spell:
                _descriptionWithNumbers.text += " заклинания";
                break;
            default:
                break;
        }
    }

    private void WriteTypeDamage(Skill skill)
    {
        switch (skill.DamageType)
        {
            case DamageType.Magical:
                _descriptionWithNumbers.text += " \nмагия";
                switch (skill.School)
                {
                    case Schools.Light:
                        _descriptionWithNumbers.text += " света";
                        break;
                    case Schools.Dark:
                        _descriptionWithNumbers.text += " тьмы";
                        break;
                    case Schools.Fire:
                        _descriptionWithNumbers.text += " огня";
                        break;
                    case Schools.Water:
                        _descriptionWithNumbers.text += " воды";
                        break;
                    case Schools.Air:
                        _descriptionWithNumbers.text += " воздуха";
                        break;
                    case Schools.Earth:
                        _descriptionWithNumbers.text += " земли";
                        break;
                    case Schools.Physical:
                        // ---
                        break;
                    case Schools.Discipline:
                        // ????
                        break;
                    case Schools.None:
                        break;
                    default:
                        break;
                }
                break;
            case DamageType.Physical:
                _descriptionWithNumbers.text += " физический";
                break;
            case DamageType.DOTPhys:
                break;
            case DamageType.DOTMag:
                break;
            case DamageType.Both:
                _descriptionWithNumbers.text += " смешанный(мы вообще его используем?)";
                break;
            case DamageType.None:
                break;
            default:
                break;
        }
    }
}
