using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace GameDescriptions.Editor
{
    [CustomPropertyDrawer(typeof(DescriptionSkillSelectorAttribute))]
    public sealed class DescriptionSkillSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var button = new Rect(position.xMax - 80f, position.y, 80f, position.height);
            var field = new Rect(position.x, position.y, position.width - 84f, position.height);

            EditorGUI.BeginChangeCheck();
            EditorGUI.ObjectField(field, property, typeof(Skill), label);
            if (EditorGUI.EndChangeCheck())
                SyncAbility(property, property.objectReferenceValue as Skill);

            var owner = FindOwner(property.serializedObject.targetObject);
            bool canSelect = owner != null && !property.serializedObject.isEditingMultipleObjects;
            using (new EditorGUI.DisabledScope(!canSelect))
            {
                var content = new GUIContent("Выбрать", canSelect
                    ? "Найти способность персонажа по названию или типу."
                    : "Выберите один талант персонажа. Для SO можно назначить компонент с префаба вручную.");
                if (GUI.Button(button, content, EditorStyles.miniButton))
                {
                    var target = property.serializedObject.targetObject;
                    string path = property.propertyPath;
                    var dropdown = new SkillDropdown(CollectSkills(owner), selected =>
                    {
                        if (target == null) return;
                        var serialized = new SerializedObject(target);
                        serialized.Update();
                        var reference = serialized.FindProperty(path);
                        if (reference == null || reference.propertyType != SerializedPropertyType.ObjectReference) return;
                        reference.objectReferenceValue = selected;
                        SyncAbility(reference, selected);
                        serialized.ApplyModifiedProperties();
                    });
                    dropdown.Show(button);
                }
            }
            EditorGUI.EndProperty();
        }

        private static Character FindOwner(UnityEngine.Object target)
        {
            if (target is Talent talent && talent.character != null) return talent.character;
            return target is Component component ? component.GetComponentInParent<Character>() : null;
        }

        private static Skill[] CollectSkills(Character owner)
        {
            var skills = new HashSet<Skill>(owner.GetComponentsInChildren<Skill>(true));
            if (owner.Abilities != null)
            {
                if (owner.Abilities.Skills != null) skills.UnionWith(owner.Abilities.Skills);
                skills.UnionWith(owner.Abilities.GetComponentsInChildren<Skill>(true));
            }
            return skills.Where(skill => skill != null)
                .OrderBy(skill => GetLabel(skill), StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static string GetLabel(Skill skill)
        {
            string name = skill.AbilityInfoHero != null ? skill.AbilityInfoHero.Name : string.Empty;
            string type = skill.GetType().Name;
            string path = AnimationUtility.CalculateTransformPath(skill.transform, null);
            return string.IsNullOrWhiteSpace(name) ? $"{type} — {path}" : $"{name} ({type}) — {path}";
        }

        private static void SyncAbility(SerializedProperty skill, Skill selected)
        {
            int dot = skill.propertyPath.LastIndexOf('.');
            if (dot < 0) return;
            var ability = skill.serializedObject.FindProperty(skill.propertyPath.Substring(0, dot) + ".Ability");
            if (ability != null) ability.objectReferenceValue = selected != null ? selected.AbilityInfoHero : null;
        }

        private sealed class SkillDropdown : AdvancedDropdown
        {
            private readonly Skill[] _skills;
            private readonly Action<Skill> _select;

            public SkillDropdown(Skill[] skills, Action<Skill> select) : base(new AdvancedDropdownState())
            {
                _skills = skills;
                _select = select;
                minimumSize = new Vector2(540f, 320f);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("Способности персонажа");
                root.AddChild(new SkillItem("Не выбрано", null));
                foreach (var skill in _skills)
                    root.AddChild(new SkillItem(GetLabel(skill), skill));
                if (_skills.Length == 0)
                    root.AddChild(new AdvancedDropdownItem("Способности не найдены") { enabled = false });
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is SkillItem skill) _select(skill.Skill);
            }
        }

        private sealed class SkillItem : AdvancedDropdownItem
        {
            public Skill Skill { get; }
            public SkillItem(string label, Skill skill) : base(label) => Skill = skill;
        }
    }
}
