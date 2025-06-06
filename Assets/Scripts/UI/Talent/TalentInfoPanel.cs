using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalentInfoPanel : MonoBehaviour
{
    [SerializeField] private TalentInfoCell _cellPref;

    private List<TalentInfoCell> _cells = new();

    public void Show(TalentData data)
    {
        gameObject.SetActive(true);

        foreach (var text in data.DescriptionsForInfoPanel)
        {
            var cell = Instantiate(_cellPref, transform);

            _cells.Add(cell);

            cell.ShowDividingLine();

            cell.Text.text = text;
        }

        if(_cells != null && _cells.Count > 0)
            _cells[^1].HideDividingLine();
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
