using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

[CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
public class SubclassSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var buttonRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        if (GUI.Button(buttonRect, GetTypeName(property), EditorStyles.popup))
        {
            ShowMenu(property);
        }

        if (property.managedReferenceValue != null)
        {
            var propertyRect = new Rect(
                position.x,
                position.y + EditorGUIUtility.singleLineHeight + 2,
                position.width,
                position.height - EditorGUIUtility.singleLineHeight - 2
            );

            EditorGUI.PropertyField(propertyRect, property, label, true);
        }

        EditorGUI.EndProperty();
    }

    void ShowMenu(SerializedProperty property)
    {
        var menu = new GenericMenu();
        var baseType = fieldInfo.FieldType;

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var type in types)
        {
            menu.AddItem(new GUIContent(type.Name), false, () =>
            {
                property.serializedObject.Update();
                property.managedReferenceValue = Activator.CreateInstance(type);
                property.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
    }

    string GetTypeName(SerializedProperty property)
    {
        return property.managedReferenceValue == null
            ? "Select Type"
            : property.managedReferenceValue.GetType().Name;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.managedReferenceValue == null)
            return EditorGUIUtility.singleLineHeight;

        return EditorGUIUtility.singleLineHeight +
               EditorGUI.GetPropertyHeight(property, true) + 2;
    }
}