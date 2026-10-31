using System;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CursorTheme_SO", menuName = "Scriptable Objects/CursorTheme_SO")]
public class CursorTheme_SO : ScriptableObject
{
    [SerializeField] public Dictionary<CursorState, CursorParameters> Cursors;

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (Cursors.Keys.Count != Enum.GetValues(typeof(CursorState)).Length)
        {
            foreach (CursorState state in Enum.GetValues(typeof(CursorState)))
                if (!Cursors.ContainsKey(state))
                    Cursors.Add(state, new CursorParameters());
        }
    }
    #endif
}

[Serializable]
public class CursorParameters
{
    public Texture2D Image;
    public Vector2 HotSpot;
}
