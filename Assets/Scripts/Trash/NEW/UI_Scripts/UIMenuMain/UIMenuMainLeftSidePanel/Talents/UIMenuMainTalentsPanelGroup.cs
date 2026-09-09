using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIMenuMainTalentsPanelGroup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UIMenuMainTalentsPanelGroupItem _talentPrefab;
    [SerializeField] private TMProLocalizer _title;
    [SerializeField] private TMProLocalizer _talentsCount;
    [SerializeField] private RectTransform _itemsParent;
    [SerializeField] private UIMenuTalentRow _rowContainer;

	private bool _isGameUI = false;
    private float _initialParentCellHeight = 65f;
    private int _itemsPerRow = 3;
    public event UnityAction OnShowPanelGroup;

    private List<UIMenuMainTalentsPanelGroupItem> _talents = new ();
    private List<UIMenuTalentRow> _rows = new ();

    private TalentsGroup _talentsGroup;
    private UIMenuMainAttributesPanel _attributesPanel;
    private TalentSystem _talentSystem;
    private Color _oldColor, _newColor;
    private TextMeshProUGUI _text;

    public event UnityAction OnTalentChanged;
    public event Action<TalentData, string> PointerEnteredOnTalentIcon;
    public event Action<TalentData> PointerExitedOnTalentIcon;
    public event UnityAction OnAnyTalentChanged;


    public void SetPanel(TalentsGroup talentsGroup, TalentSystem talentSystem, UIMenuMainAttributesPanel attributesPanel, bool isGameUI, bool isInteractable = true)
    {
        _isGameUI = isGameUI;

        _talentSystem = talentSystem;
        _attributesPanel = attributesPanel;
        _talentsGroup = talentsGroup;
        _title.Localize(talentsGroup.Name);

        if (_title.gameObject.TryGetComponent<TextMeshProUGUI>(out var text))
        {
            _oldColor = text.color;
            _newColor = new Color(255, 255, 141);
            _text = text;
        }

        UpdateActiveTalentsCount();

        for (int i = 0; i < talentsGroup.TalentRows.Count; i++)
        {
            var row = Instantiate(_rowContainer, _itemsParent);
            _rows.Add(row);

            foreach (var item in talentsGroup.TalentRows[i].Talents)
            {
                var talent = Instantiate(_talentPrefab, row.Rect);

                talent.Owner = this;
                talent.Fill(item, i, isInteractable);

                if (isInteractable)
                    talent.Selected += OnTalentSelected;

                talent.PointerEntered += OnPointerEnteredOnTalentIcon;
                talent.PointerExited += OnPointerExitedOnTalentIcon;

                row.AddTalent(talent);
                _talents.Add(talent);
            }

            row.SetConditionDescription(talentsGroup.GetRowConditionDescription(i));
        }

        RefreshRowsLocked();
    }
    
    public void RefreshRowsLocked()
    {
        foreach (var talentIcon in _talents)
            talentIcon.RefreshLockState();
    }
    
    private void OnEnable()
    {
        SaveManager.Instance.TalentStateSettled += RefreshAllIconsVisual;
    }

    private void OnDisable()
    {
        SaveManager.Instance.TalentStateSettled -= RefreshAllIconsVisual;

        foreach (var talent in _talents)
        {
            talent.Selected -= OnTalentSelected;
            talent.PointerEntered -= OnPointerEnteredOnTalentIcon;
            talent.PointerExited -= OnPointerExitedOnTalentIcon;
        }
    }

    void UpdateActiveTalentsCount()
    {
        //var activeTalentsCount = _talentsGroup.TalentsData.Count(o => o.Data.IsOpen);
        var activeTalentsCount = GetCountTalents();

		_talentsCount.ChangeKey(activeTalentsCount);
    }
    
    private void RefreshAllIconsVisual()
    {
        foreach (var talentIcon in _talents)
            talentIcon.TryRefreshVisual();

        RefreshRowsLocked();
        UpdateActiveTalentsCount();
    }

    private void RefreshTalentsVisual()
    {
        foreach (var talentItem in _talents)
        {
            talentItem.TryRefreshVisual(); 
        }
    }

    void OnTalentSelected(TalentData talent, bool isOpen, int lvl)
    {
        SaveManager.Instance.SaveTalent(_talentsGroup.ID, talent.Row, talent.Name, isOpen, lvl);
        UpdateActiveTalentsCount();
        _attributesPanel.UpdateAttributesPoints();
        RefreshTalentsVisual();
        RefreshRowsLocked();
        OnTalentChanged?.Invoke();
        OnAnyTalentChanged?.Invoke();
    }

	private int GetActiveTalents()
	{
		List<Talent> activeTalents = new();
		
			foreach (TalentRow row in _talentsGroup.TalentRows)
			{
				foreach (Talent talent in row.Talents)
				{
					if (talent.Data.IsOpen)
					{
						activeTalents.Add(talent);
					}
				}
			}

		return activeTalents.Count;

	}

    private int GetCountTalents()
    {
        List<Talent> activeTalents = new();
        int count = 0;
        foreach (TalentRow row in _talentsGroup.TalentRows)
        {
            foreach (Talent talent in row.Talents)
            {
                if (talent.Data.IsOpen)
                {
                    count += talent.Data.Level;
                }
            }
        }

        return count;
    }

    private int GetItemsInRowCount()
    {
        int rows = 0;
		
        foreach (TalentRow row in _talentsGroup.TalentRows)
        {
            foreach (Talent talent in row.Talents)
            {
                rows++;
            }
        }
		

        return rows;
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

    private void OnPointerEnteredOnTalentIcon(TalentData talent, string rowCondition)
    {
        PointerEnteredOnTalentIcon?.Invoke(talent, rowCondition);
    }

    private void OnPointerExitedOnTalentIcon(TalentData talent)
    {
        PointerExitedOnTalentIcon?.Invoke(talent);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(_text != null)
        {
            _text.color = _newColor;
        }
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_text != null)
        {
            _text.color = _oldColor;
        }
    }
}