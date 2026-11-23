using System;
using System.Collections.Generic;
using System.Linq;
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

        public List<DescriptionRow> Build(Character owner = null, Skill skill = null)
        {
            var context = new DescriptionContext(owner, skill);
            var rows = new List<DescriptionRow>();
            Append(rows, context);
            return rows;
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

    public sealed class DescriptionContext
    {
        public Character Owner { get; }
        public Skill Skill { get; }
        internal HashSet<AbilityInfo> ActiveAbilities { get; } = new();

        public DescriptionContext(Character owner, Skill skill)
        {
            Owner = owner != null ? owner : skill != null ? skill.Hero : null;
            Skill = skill;
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
            => DescriptionTable.AddText(rows, Text);
    }

    [Serializable]
    public sealed class LocString : DescriptionEntry
    {
        public LocalizedString Text = new();
        public override void Append(List<DescriptionRow> rows, DescriptionContext context)
        {
            if (Text != null && !Text.IsEmpty)
                DescriptionTable.AddText(rows, Text.GetLocalizedString());
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
            if (State == null) return;
            CharacterState.enumToState.TryGetValue(State.State, out var values);
            string name = DescriptionLocalization.GetString(State.Name, values);
            string description = DescriptionLocalization.GetString(State.Description, values);
            DescriptionTable.AddText(rows, $"<color={DescriptionTextProcessor.ColorMap[DescriptionTextProcessor.ColorPallete.Y]}>{name}</color> {description}");
            string tooltip = DescriptionLocalization.GetString(State.Tooltip, values);
            if (tooltip != description)
                DescriptionTable.AddText(rows, tooltip);
        }
    }
}
