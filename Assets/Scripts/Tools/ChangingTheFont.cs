using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChangingTheFont : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset _newFont;

#if UNITY_EDITOR
    [ContextMenu("Change TMP Font")]
    private void ChangeAll()
    {
        var labels = GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (var label in labels)
        {
            Undo.RecordObject(label, "Change TMP Font");
            label.font = _newFont;

            EditorUtility.SetDirty(label);
        }
    }
#endif
}
