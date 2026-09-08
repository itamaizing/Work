using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

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
        var baseType = GetDeclaredType(property);

        if (baseType == null)
        {
            Debug.LogWarning($"[SubclassSelectorDrawer] Не удалось определить тип для {property.propertyPath}");
            return;
        }

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        menu.AddItem(new GUIContent("None"), property.managedReferenceValue == null, () =>
        {
            property.serializedObject.Update();
            property.managedReferenceValue = null;
            property.serializedObject.ApplyModifiedProperties();
        });

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
    
    static Type GetDeclaredType(SerializedProperty property)
    {
        string typeName = property.managedReferenceFieldTypename;
        if (string.IsNullOrEmpty(typeName)) return null;

        int splitIndex = typeName.IndexOf(' ');
        if (splitIndex < 0) return null;

        string assemblyName = typeName.Substring(0, splitIndex);
        string className = typeName.Substring(splitIndex + 1);

        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);

        return assembly?.GetType(className);
    }

    static Type[] SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
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