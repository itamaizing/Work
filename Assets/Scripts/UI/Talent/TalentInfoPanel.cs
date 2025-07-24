using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TalentInfoPanel : MonoBehaviour
{
    [SerializeField] private TalentInfoCell _cellPref;
    [SerializeField] private TMP_Text _name;

    private List<TalentInfoCell> _cells = new();

    public void Show(TalentData data)
    {
        gameObject.SetActive(true);
        bool hasStateInfos = data.StateInfos.Count > 0;

        foreach (var text in data.DescriptionsForInfoPanel)
        {
            var cell = Instantiate(_cellPref, transform);
            _cells.Add(cell);

            cell.ShowDividingLine();
            cell.TextDescription.text = text;
        }

        if (!hasStateInfos && _cells.Count > 0) _cells[^1].HideDividingLine();

        foreach (var st in data.StateInfos)
        {
            var cell = Instantiate(_cellPref, transform);
            _cells.Add(cell);

            cell.HideDividingLine();
            cell.TextDescription.text = $"<color=#FFFF00>{st.StateName}</color> - {st.Description}";
        }

        _name.text = data.Description;
    }

    public void Hide()
    {
        gameObject.SetActive(false);

        if (_cells == null)
            return;

        foreach (var cell in _cells)
            Destroy(cell.gameObject);

        _cells.Clear();
    }
}
