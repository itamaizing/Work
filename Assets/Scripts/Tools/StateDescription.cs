using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace GameDescriptions
{
    public static class StateDescription
    {
        private static readonly Dictionary<States, StateData_SO> Data = new();
        private static readonly LocalizedString RemainingTime = new("States", "Common.RemainingDuration");
        private static readonly LocalizedString Stacks = new("States", "Common.Stacks");
        private static bool _loaded;

        public static string GetSchoolText(Schools school)
        {
            if (school == Schools.None) return string.Empty;
            string text = DescriptionLocalization.Tooltip($"School.{school}.Name");
            return DescriptionTextProcessor.TryResolveColor(school.ToString(), out var color)
                ? $"<color={color}>{text}</color>" : text;
        }

        public static string GetDispelText(DispelType type)
        {
            if (type == DispelType.None) return DescriptionLocalization.Tooltip("Dispel.None");
            var names = new List<string>();
            if ((type & DispelType.Magic) != 0) names.Add(DescriptionLocalization.Tooltip("Dispel.Magic"));
            if ((type & DispelType.Physic) != 0) names.Add(DescriptionLocalization.Tooltip("Dispel.Physic"));
            if ((type & DispelType.Immaterial) != 0) names.Add(DescriptionLocalization.Tooltip("Dispel.Immaterial"));
            return names.Count > 0 ? string.Join(DescriptionLocalization.Tooltip("List.Alternatives"), names) : type.ToString();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Data.Clear();
            _loaded = false;
        }

        public static void Register(StateData_SO data)
        {
            if (data != null) Data[data.State] = data;
        }

        public static void Unregister(StateData_SO data)
        {
            if (data != null && Data.TryGetValue(data.State, out var current) && current == data)
                Data.Remove(data.State);
        }

        public static StateData_SO Find(States state)
        {
            if (!_loaded)
            {
                _loaded = true;
                foreach (var data in Resources.LoadAll<StateData_SO>("StateData")) Register(data);
            }
            return Data.TryGetValue(state, out var result) ? result : null;
        }

        public static string GetDurationText(float remaining, float maximum)
        {
            if (float.IsNaN(remaining)) return string.Empty;
            if (remaining < 0f || float.IsInfinity(remaining)) return DescriptionLocalization.Tooltip("Value.InfiniteDuration");
            string time = DescriptionLocalization.Number(remaining, "0.#");
            if (maximum >= 0f && !float.IsInfinity(maximum))
                time = DescriptionLocalization.Tooltip("Format.Pair", time, DescriptionLocalization.Number(maximum, "0.#"));
            return DescriptionLocalization.Tooltip("Value.Time", time);
        }

        public static void GetTooltip(StateBasic state, out string name, out string description, bool includeRemainingTime = true)
        {
            name = string.Empty;
            description = string.Empty;
            if (state == null) return;

            var context = new DescriptionContext(state.Target, state.Skill, state).WithValues(state, false);
            var data = Find(state.State);
            var rows = new List<DescriptionRow>();
            name = state.State.ToString();
            if (data != null && context.ActiveStates.Add(data))
            {
                try
                {
                    string localizedName = DescriptionLocalization.GetString(data.Name, context);
                    if (!string.IsNullOrWhiteSpace(localizedName))
                        name = DescriptionTextProcessor.Process(localizedName);
                    AppendBody(rows, data, context);
                }
                finally
                {
                    context.ActiveStates.Remove(data);
                }
            }

            if (includeRemainingTime && state.RemainingDuration >= 0f)
                DescriptionTable.AddText(rows, DescriptionLocalization.GetString(RemainingTime, context));
            if (state.MaxStacksCount > 1 && state.MaxStacksCount < int.MaxValue)
                DescriptionTable.AddText(rows, DescriptionLocalization.GetString(Stacks, context));
            description = DescriptionTable.ToText(rows);
        }

        private static void AppendBody(List<DescriptionRow> rows, StateData_SO data, DescriptionContext context)
        {
            string description = DescriptionLocalization.GetString(data.Description, context);
            DescriptionTable.AddText(rows, description);
            string tooltip = DescriptionLocalization.GetString(data.Tooltip, context);
            if (tooltip != description) DescriptionTable.AddText(rows, tooltip);
        }

        internal static DescriptionContext GetContext(StateData_SO state, DescriptionContext context)
        {
            var values = context?.Values as StateBasic;
            if (values == null || values.State != state.State)
            {
                CharacterState.enumToState.TryGetValue(state.State, out values);
                if (values == null && (state.State == States.IceDeathSeriesPhysical || state.State == States.IceDeathSeriesMagic))
                    values = new IceDeathSeriesState(state.State);
                if (values == null && state.State == States.BlockCooldown)
                    values = new LocalVisualState(state.State, 0d);
            }
            return (context ?? new DescriptionContext()).WithValues(values, false);
        }

        public static void Append(List<DescriptionRow> rows, StateData_SO state, DescriptionContext context = null)
        {
            if (state == null) return;
            context = GetContext(state, context);
            if (!context.ActiveStates.Add(state)) return;
            try
            {
                string name = DescriptionLocalization.GetString(state.Name, context);
                string description = DescriptionLocalization.GetString(state.Description, context);
                DescriptionTable.AddText(rows,
                    $"%%link=State[{name}] {description}");
                string tooltip = DescriptionLocalization.GetString(state.Tooltip, context);
                if (tooltip != description) DescriptionTable.AddText(rows, tooltip);
            }
            finally
            {
                context.ActiveStates.Remove(state);
            }
        }
    }
}
