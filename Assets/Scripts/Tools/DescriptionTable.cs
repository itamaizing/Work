using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;

namespace GameDescriptions
{
    [Serializable]
    public sealed class DescriptionTable
    {
        [SerializeReference, SubclassSelector]
        private List<DescriptionEntry> _entries = new();

        public List<DescriptionEntry> Entries => _entries;
        public bool HasEntries => _entries != null && _entries.Count > 0;

        public List<DescriptionRow> Build(Character owner = null, Skill skill = null, IDescriptionValue values = null)
        {
            return DescriptionBuilder.BuildRows(this, new DescriptionContext(owner, skill, values));
        }

        internal void Append(List<DescriptionRow> rows, DescriptionContext context)
        {
            if (_entries == null) return;
            foreach (var entry in _entries)
                entry?.Append(rows, context);
        }

        public static void AddText(List<DescriptionRow> rows, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            rows.AddRange(DescriptionTextProcessor.ProcessRows(text));
        }

        public static string ToText(IEnumerable<DescriptionRow> rows)
        {
            return string.Join("\n", rows.Select(row => row.IsLineBreak ? "────────────────────" : row.Text));
        }
    }

    public readonly struct DescriptionRow
    {
        public string Text { get; }
        public bool IsLineBreak { get; }
        public float LeftPadding { get; }
        public float RightPadding { get; }

        public DescriptionRow(string text, bool isLineBreak = false, float leftPadding = 0f, float rightPadding = 0f)
        {
            Text = text;
            IsLineBreak = isLineBreak;
            LeftPadding = Math.Max(0f, leftPadding);
            RightPadding = Math.Max(0f, rightPadding);
        }

        public static DescriptionRow LineBreak(float leftPadding = 0f, float rightPadding = 0f)
            => new DescriptionRow(string.Empty, true, leftPadding, rightPadding);
    }

    public sealed class DescriptionContext : IDescriptionValue
    {
        public Character Owner { get; }
        public Skill Skill { get; }
        public IDescriptionValue Values { get; }
        public CultureInfo Culture { get; }
        public bool HasValues => Values != null || Skill is IDescriptionValue || Owner is IDescriptionValue;
        internal HashSet<AbilityInfo> ActiveAbilities { get; }
        internal HashSet<StateData_SO> ActiveStates { get; }
        private readonly bool _useFallbackValues;

        public DescriptionContext(Character owner = null, Skill skill = null, IDescriptionValue values = null,
            CultureInfo culture = null)
            : this(owner, skill, values, culture, new HashSet<AbilityInfo>(), new HashSet<StateData_SO>(), true) { }

        private DescriptionContext(Character owner, Skill skill, IDescriptionValue values, CultureInfo culture,
            HashSet<AbilityInfo> activeAbilities, HashSet<StateData_SO> activeStates, bool useFallbackValues)
        {
            Owner = owner != null ? owner : skill != null ? skill.Hero : null;
            Skill = skill;
            Values = values;
            Culture = culture;
            ActiveAbilities = activeAbilities;
            ActiveStates = activeStates;
            _useFallbackValues = useFallbackValues;
        }

        public DescriptionContext WithSkill(Skill skill)
            => new DescriptionContext(Owner, skill, skill as IDescriptionValue, Culture, ActiveAbilities, ActiveStates, true);

        public DescriptionContext WithValues(IDescriptionValue values, bool useFallbackValues = true)
            => new DescriptionContext(Owner, Skill, values, Culture, ActiveAbilities, ActiveStates, useFallbackValues);

        public bool TryGetDescriptionValue(string key, out object value)
        {
            if (Values != null && Values.TryGetDescriptionValue(key, out value)) return true;
            if (_useFallbackValues && Skill is IDescriptionValue skill && !ReferenceEquals(skill, Values) &&
                skill.TryGetDescriptionValue(key, out value)) return true;
            if (_useFallbackValues && Owner is IDescriptionValue owner && !ReferenceEquals(owner, Values) &&
                owner.TryGetDescriptionValue(key, out value)) return true;
            value = null;
            return false;
        }

        public Skill Resolve(AbilityInfo info)
        {
            if (info == null) return null;
            if (Skill != null && Skill.AbilityInfoHero == info) return Skill;
            var registered = Owner != null ? Owner.Abilities?.Skills : null;
            var result = registered?.FirstOrDefault(candidate => candidate != null && candidate.AbilityInfoHero == info);
            if (result != null) return result;
            return Owner == null ? null : Owner.GetComponentsInChildren<Skill>(true)
                .FirstOrDefault(candidate => candidate.AbilityInfoHero == info);
        }
    }

    [Serializable]
    public abstract class DescriptionEntry
    {
        public abstract void Append(List<DescriptionRow> rows, DescriptionContext context);
    }

    [Serializable]
    public sealed class PlainText : DescriptionEntry
    {
        [TextArea] public string Text;
        public override void Append(List<DescriptionRow> rows, DescriptionContext context)
            => DescriptionTable.AddText(rows, DescriptionBuilder.FormatText(Text, context));
    }

    [Serializable]
    public sealed class LocString : DescriptionEntry
    {
        public LocalizedString Text = new();
        public override void Append(List<DescriptionRow> rows, DescriptionContext context)
        {
            if (Text != null && !Text.IsEmpty)
                DescriptionTable.AddText(rows, DescriptionLocalization.GetString(Text, context));
        }
    }

    [Serializable]
    public sealed class LineBreak : DescriptionEntry
    {
        [Min(0f), Tooltip("Дополнительный отступ слева.")]
        public float LeftPadding;
        [Min(0f), Tooltip("Дополнительный отступ справа.")]
        public float RightPadding;

        public override void Append(List<DescriptionRow> rows, DescriptionContext context)
            => rows.Add(DescriptionRow.LineBreak(LeftPadding, RightPadding));
    }

    [Serializable]
    public sealed class SkillLink : DescriptionEntry
    {
        [Tooltip("Для таблиц в ScriptableObject указать ресурс AbilityInfo. Соответствующая способность будет найдена у персонажа, которому принадлежит описание.")]
        public AbilityInfo Ability;
        [DescriptionSkillSelector, Tooltip("Выбрать способность персонажа кнопкой «Выбрать» или назначь её компонент вручную (неудобно).")]
        public Skill Skill;

        public override void Append(List<DescriptionRow> rows, DescriptionContext context)
        {
            var info = Ability != null ? Ability : Skill != null ? Skill.AbilityInfoHero : null;
            var resolved = context.Resolve(info);
            var fallback = Skill != null && Skill.AbilityInfoHero == info ? Skill : null;
            SkillDescription.Append(rows, resolved != null ? resolved : fallback, info, context, true);
        }
    }

    [Serializable]
    public sealed class StateLink : DescriptionEntry
    {
        public StateData_SO State;

        public override void Append(List<DescriptionRow> rows, DescriptionContext context)
        {
            StateDescription.Append(rows, State, context);
        }
    }
}
