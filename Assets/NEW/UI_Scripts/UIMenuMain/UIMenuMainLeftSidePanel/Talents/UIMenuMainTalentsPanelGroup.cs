using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class UIMenuMainTalentsPanelGroup : MonoBehaviour
{
    [ReadOnly,ShowInInspector]
    public UIMenuMainTalentsPanel Owner;
    
    [SerializeField] private UIMenuMainTalentsPanelGroupItem _talentPrefab;
    [SerializeField] private TMProLocalizer _title;
    [SerializeField] private TMProLocalizer _talentsCount;
    [SerializeField] private RectTransform _itemsParent;

    private List<UIMenuMainTalentsPanelGroupItem> _talents = new ();

    private TalentsGroup _talentsGroup;

    public void SetPanel(TalentsGroup talentsGroup)
    {
        _talentsGroup = talentsGroup;
        _title.Localize(talentsGroup.Name);

        UpdateActiveTalentsCount();

        foreach (var item in talentsGroup.TalentsData)
        {
            var talent = Instantiate(_talentPrefab, _itemsParent);
            
            talent.Owner = this;
            talent.Fill(item.Data);
            talent.Selected += OnTalentSelected;
            
            _talents.Add(talent);
        }
    }

    void UpdateActiveTalentsCount()
    {
        var activeTalentsCount = _talentsGroup.TalentsData.Count(o => o.Data.IsOpen);
        _talentsCount.ChangeKey(activeTalentsCount);
    }

    void OnTalentSelected(TalentData talent, bool isOpen)
    { 
        SaveManager.Instance.SaveTalent(_talentsGroup.ID, talent.Id, isOpen);
        SaveManager.Instance.LoadTalent(_talentsGroup.ID, talent.Id);

        UpdateActiveTalentsCount();
        Owner.Owner.UpdateAttributes();
    }

    public void Show()
    {
        if(Owner == null) return;

        if (_itemsParent.gameObject.activeInHierarchy == false)
        {
            Owner.HidePanels();
            _itemsParent.gameObject.SetActive(true);
        }
        else
        {
            Owner.HidePanels();
        }
    }
    
    public void Hide()
    {
        _itemsParent.gameObject.SetActive(false);
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }
}
