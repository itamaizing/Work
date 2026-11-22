using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "StateData_SO", menuName = "Scriptable Objects/StateData_SO")]
public class StateData_SO : ScriptableObject
{
    public States State;
    public LocalizedString Name;
    public LocalizedString Description;
    public LocalizedString Tooltip;

    public StateBasic StateScript => CharacterState.enumToState[State];

#if UNITY_EDITOR
    [MenuItem("Tools/Generate Missing StateData")]
    public static void GenerateMissingStateData()
    {
        string DataFolder = "Assets/Resources/StateData/";
        string LocTable = "States";

        if (!AssetDatabase.IsValidFolder(DataFolder))
        {
            Debug.LogError("Missing StateData folder");
            return;
        }

        var stateTypes = Enum.GetValues(typeof(States));

        foreach (States state in stateTypes)
        {
            //string stateName = type.Name;
            if (AssetDatabase.LoadAssetAtPath($"{DataFolder}/{state}.asset", typeof(SO_ResourceData)) != null)
                continue;

            string assetPath = $"{DataFolder}/{state}.asset";

            var data = ScriptableObject.CreateInstance<StateData_SO>();
            data.State = state;
            data.Name = new LocalizedString(LocTable, $"{state}.Name");
            data.Description = new LocalizedString(LocTable, $"{state}.Desc");
            data.Tooltip = new LocalizedString(LocTable, $"{state}.Tooltip");

            AssetDatabase.CreateAsset(data, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
#endif
}
