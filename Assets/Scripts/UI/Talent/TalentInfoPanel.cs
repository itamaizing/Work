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

        foreach (var text in data.DescriptionsForInfoPanel)
        {
            var cell = Instantiate(_cellPref, transform);

            _cells.Add(cell);

            cell.ShowDividingLine();

            cell.TextDescription.text = text;
        }

        if(_cells != null && _cells.Count > 0)
            _cells[^1].HideDividingLine();

        _name.text = data.Name;
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
