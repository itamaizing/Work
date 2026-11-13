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
        gameObject.SetActive(true);
        _name.text = data.Description;

        int descCnt = data.DescriptionsForInfoPanel.Count;
        int stateCnt = data.StateInfos.Count;
        int pairCnt = Mathf.Max(descCnt, stateCnt);
        TalentInfoCell cell = null;

        //Condition TEXT
        var conditionCell = Instantiate(_cellPref, transform);
        _cells.Add(conditionCell);
        
        string fullCondition = string.IsNullOrEmpty(rowConditionDescription)
            ? data.ConditionDescription
            : string.IsNullOrEmpty(data.ConditionDescription)
                ? rowConditionDescription
                : $"{data.ConditionDescription}\n{rowConditionDescription}";

        conditionCell.TextDescription.text = fullCondition;
        conditionCell.ShowDividingLine();

        //Condition TEXT

        for (int i = 0; i < pairCnt; i++)
        {
            if (i < descCnt)
            {
                var descCell = Instantiate(_cellPref, transform);
                cell = descCell;
                _cells.Add(descCell);

                string descString = DescriptionTextProcessor.Process(data.DescriptionsForInfoPanel[i]);
                
                descCell.TextDescription.text = descString;
                descCell.ShowDividingLine();
                if (i == pairCnt - 1) descCell.HideDividingLine();
            }

            if (i < stateCnt)
            {
                var st = data.StateInfos[i];

                var stateCell = Instantiate(_cellPref, transform);
                _cells.Add(stateCell);

                if (st.StateName != "" || st.Description != "")
                {
                    string description = DescriptionTextProcessor.Process(st.Description);
                    stateCell.TextDescription.text =
                        $"<color={DescriptionTextProcessor.ColorMap[DescriptionTextProcessor.ColorPallete.Y]}>{st.StateName}</color> {description}";
                }
                else
                {
                    stateCell.TextDescription.text = "";
                }
                cell.HideDividingLine();
                stateCell.ShowDividingLine();
                if (i == pairCnt - 1) stateCell.HideDividingLine();
            }
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
        if (_cells == null) return;
        foreach (var cell in _cells) Destroy(cell.gameObject);
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