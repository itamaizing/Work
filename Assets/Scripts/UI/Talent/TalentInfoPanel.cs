using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TalentInfoPanel : MonoBehaviour
{
    [SerializeField] private TalentInfoCell _cellPref;
    [SerializeField] private TMP_Text _name;
    [SerializeField] private bool _followCursor;
    [SerializeField] private float _cursorGap = 12f;

    private List<TalentInfoCell> _cells = new();
    
    private readonly Vector3[] _corners = new Vector3[4];

    private void LateUpdate()
    {
        if (_followCursor)
            PositionAtCursor();
    }

    public void Show(TalentData data, string rowConditionDescription = "")
    {
        ClearCells();
        gameObject.SetActive(true);
        _name.text = data.Description;

        string fullCondition = string.IsNullOrEmpty(rowConditionDescription)
            ? data.ConditionDescription
            : string.IsNullOrEmpty(data.ConditionDescription)
                ? rowConditionDescription
                : $"{data.ConditionDescription}\n{rowConditionDescription}";

        var rows = new List<GameDescriptions.DescriptionRow>();
        GameDescriptions.DescriptionTable.AddText(rows, fullCondition);
        rows.AddRange(data.DescriptionTable.Build(data.DescriptionOwner, values: data.DescriptionValues));

        foreach (var row in rows)
        {
            var cell = Instantiate(_cellPref, transform);
            _cells.Add(cell);
            cell.Show(row);
        }

        if (_followCursor)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            PositionAtCursor();
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        ClearCells();
    }

    private void ClearCells()
    {
        foreach (var cell in _cells)
        {
            cell.gameObject.SetActive(false);
            Destroy(cell.gameObject);
        }
        _cells.Clear();
    }

    private void PositionAtCursor()
    {
        var tooltip = (RectTransform)transform;
        var parent = (RectTransform)tooltip.parent;

        tooltip.GetWorldCorners(_corners);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, _corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, _corners[2]);
        Vector2 size = topRight - bottomLeft;

        Vector2 mouse = Input.mousePosition;
        Rect bounds = Screen.safeArea;

        float x = mouse.x + _cursorGap;
        float y = mouse.y;

        if (x + size.x > bounds.xMax)
            x = mouse.x - _cursorGap - size.x;

        x = Mathf.Clamp(x, bounds.xMin, bounds.xMax - size.x);
        y = Mathf.Clamp(y, bounds.yMin + size.y/2, bounds.yMax - size.y/2);

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parent, new Vector2(x, y), null, out Vector3 position))
        {
            tooltip.position = position;
        }
    }
}
