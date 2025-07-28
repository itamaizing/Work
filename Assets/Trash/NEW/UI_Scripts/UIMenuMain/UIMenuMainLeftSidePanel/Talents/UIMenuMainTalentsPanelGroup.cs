using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class UIMenuMainTalentsPanelGroup : MonoBehaviour
{
    [SerializeField] private UIMenuMainTalentsPanelGroupItem _talentPrefab;
    [SerializeField] private TMProLocalizer _title;
    [SerializeField] private TMProLocalizer _talentsCount;
    [SerializeField] private RectTransform _itemsParent;

    private bool _isGameUI = false;
    public event UnityAction OnShowPanelGroup;

    private List<UIMenuMainTalentsPanelGroupItem> _talents = new ();

    private TalentsGroup _talentsGroup;
    private UIMenuMainAttributesPanel _attributesPanel;

    public event Action<TalentData> PointerEnteredOnTalentIcon;
    public event Action<TalentData> PointerExitedOnTalentIcon;

    public void SetPanel(TalentsGroup talentsGroup, UIMenuMainAttributesPanel attributesPanel, bool isGameUI, bool isInteractable = true)
    {
        _isGameUI = isGameUI;
        
        _attributesPanel = attributesPanel;
        _talentsGroup = talentsGroup;
        _title.Localize(talentsGroup.Name);

        UpdateActiveTalentsCount();

        foreach (var item in talentsGroup.TalentsData)
        {
            var talent = Instantiate(_talentPrefab, _itemsParent);
            
            talent.Owner = this;
            talent.Fill(item.Data);

            talent.Button.interactable = isInteractable;
            
            talent.Selected += OnTalentSelected;
            talent.PointerEntered += OnPointerEnteredOnTalentIcon;
            talent.PointerExited += OnPointerExitedOnTalentIcon;


            _talents.Add(talent);
        }
    }
    
    private void OnDisable()
    {
        foreach (var talent in _talents)
        {
            talent.Selected -= OnTalentSelected;
            talent.PointerEntered -= OnPointerEnteredOnTalentIcon;
            talent.PointerExited -= OnPointerExitedOnTalentIcon;
        }
    }

    void UpdateActiveTalentsCount()
    {
        var activeTalentsCount = _talentsGroup.TalentsData.Count(o => o.Data.IsOpen);
        _talentsCount.ChangeKey(activeTalentsCount);
    }

    void OnTalentSelected(TalentData talent, bool isOpen)
    {
		Debug.Log("Talent selected in MAIN" + talent);
		SaveManager.Instance.SaveTalent(_talentsGroup.ID, talent.Name, isOpen);
        SaveManager.Instance.LoadTalent(_talentsGroup.ID, talent.Name, _isGameUI);

        UpdateActiveTalentsCount();
        _attributesPanel.UpdateAttributesPoints();
    }

    public void Show()
    {
        if (_itemsParent.gameObject.activeInHierarchy == false)
        {
            OnShowPanelGroup?.Invoke();
            _itemsParent.gameObject.SetActive(true);
        }
        else
        {
            OnShowPanelGroup?.Invoke();
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

    private void OnPointerEnteredOnTalentIcon(TalentData talent)
    {
        PointerEnteredOnTalentIcon?.Invoke(talent);
    }

    private void OnPointerExitedOnTalentIcon(TalentData talent)
    {
        PointerExitedOnTalentIcon?.Invoke(talent);
    }
}