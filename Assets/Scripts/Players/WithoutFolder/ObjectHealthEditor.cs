#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectHealth), true)]
public class ObjectHealthEditor : Editor
{
    private SerializedProperty _dbProp;
    private SerializedProperty _nameProp;
    private string[] _options;

    private void OnEnable()
    {
        _dbProp = serializedObject.FindProperty("_abilityBanDatabase");
        _nameProp = serializedObject.FindProperty("_selectedAbilityName");

        TryAutoAssignDatabase();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "_selectedAbilityName");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("AbilityBan", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(_dbProp);

        if (_dbProp.objectReferenceValue is AbilityBanDatabase db && db.abilityNames.Count > 0)
        {
            _options = db.abilityNames.ToArray();
            int currentIndex = Mathf.Max(0, System.Array.IndexOf(_options, _nameProp.stringValue));
            int newIndex = EditorGUILayout.Popup("Skill Class", currentIndex, _options);
            _nameProp.stringValue = _options[newIndex];
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void TryAutoAssignDatabase()
    {
        if (_dbProp != null && _dbProp.objectReferenceValue == null)
        {
            var found = Resources.Load<AbilityBanDatabase>("AbilityBanDatabase");
            if (found != null)
            {
                _dbProp.objectReferenceValue = found;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif
