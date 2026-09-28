#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AudioKeyList))]
public class AudioKeyListDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var keysProp = property.FindPropertyRelative("Keys");
        if (keysProp == null || !keysProp.isArray)
        {
            EditorGUI.HelpBox(position, "Keys field not found", MessageType.Error);
            EditorGUI.EndProperty();
            return;
        }

        float lineH = EditorGUIUtility.singleLineHeight;
        float y = position.y;

        string enumLabel = GetSfxTypeLabel(property);
        EditorGUI.LabelField(new Rect(position.x, y, position.width, lineH), enumLabel, EditorStyles.boldLabel);
        y += lineH + 2;

        var database = FindDatabase(property);
        string[] options = GetAllKeys(database);

        for (int i = 0; i < keysProp.arraySize; i++)
        {
            var element = keysProp.GetArrayElementAtIndex(i);
            float w = position.width - 28;

            Rect popupRect = new Rect(position.x, y, w, lineH);
            Rect btnRect   = new Rect(position.x + w + 2, y, 26, lineH);

            DrawPopup(popupRect, element, options);

            if (GUI.Button(btnRect, "−"))
            {
                keysProp.DeleteArrayElementAtIndex(i);
                break;
            }

            y += lineH + 2;
        }

        if (GUI.Button(new Rect(position.x, y, 110, lineH), "+ Add Clip"))
        {
            keysProp.arraySize++;
            var last = keysProp.GetArrayElementAtIndex(keysProp.arraySize - 1);
            last.stringValue = options.Length > 1 ? options[1] : "";
        }

        EditorGUI.EndProperty();
    }

    private string GetSfxTypeLabel(SerializedProperty clipsProperty)
    {
        string path = clipsProperty.propertyPath;
        int lastDot = path.LastIndexOf('.');
        if (lastDot < 0) return "Sfx";

        string entryPath = path.Substring(0, lastDot);

        var typeProp = clipsProperty.serializedObject.FindProperty(entryPath + ".Type");
        if (typeProp == null) return "Sfx";

        var enumValues = System.Enum.GetValues(typeof(Sfx_Skill));
        int enumIndex = typeProp.enumValueIndex;
        return enumIndex >= 0 && enumIndex < enumValues.Length
            ? enumValues.GetValue(enumIndex).ToString()
            : "Sfx";
    }

    private void DrawPopup(Rect rect, SerializedProperty prop, string[] options)
    {
        string current = prop.stringValue ?? "";
        int idx = System.Array.IndexOf(options, current);
        if (idx < 0) idx = 0;

        int newIdx = EditorGUI.Popup(rect, idx, options);
        if (newIdx != idx && newIdx >= 0 && newIdx < options.Length)
            prop.stringValue = options[newIdx];
    }

    private string[] GetAllKeys(SoundDatabase db)
    {
        var list = new List<string> { "" };

        if (db == null)
        {
            list.Add("(назначьте SoundDatabase на компоненте)");
            return list.ToArray();
        }

        try
        {
            var keys = db.GetAllKeys();
            if (keys != null) list.AddRange(keys);
        }
        catch
        {
        }

        return list.Distinct().OrderBy(k => k).ToArray();
    }
    
    private SoundDatabase FindDatabase(SerializedProperty clipsProperty)
    {
        string entriesFieldPath = GetParentPath(clipsProperty.propertyPath);
        string ownerPath = GetParentPath(entriesFieldPath);

        if (string.IsNullOrEmpty(ownerPath)) return null;

        var dbProp = clipsProperty.serializedObject.FindProperty(ownerPath + "._database");
        return dbProp?.objectReferenceValue as SoundDatabase;
    }
    
    private static string GetParentPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        int lastDot = path.LastIndexOf('.');
        if (lastDot < 0) return null;

        string parent = path.Substring(0, lastDot);

        if (parent.EndsWith("]"))
        {
            int idx = parent.LastIndexOf(".Array.data[");
            if (idx >= 0)
                parent = parent.Substring(0, idx);
        }

        return parent;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var keysProp = property.FindPropertyRelative("Keys");
        int count = (keysProp != null && keysProp.isArray) ? keysProp.arraySize : 0;
        return (count + 2) * (EditorGUIUtility.singleLineHeight + 2) + 4;
    }
}
#endif