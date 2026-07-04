using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIMenuMainTalentsPanelGroupItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public event UnityAction<TalentData, bool, int> Selected;
    public event UnityAction<TalentData> PointerEntered;
    public event UnityAction<TalentData> PointerExited;
    

    [ReadOnly,ShowInInspector]
    public UIMenuMainTalentsPanelGroup Owner;
    
    [SerializeField] private UITwoStates activeState;
    [SerializeField] private Image activeImage;
    [SerializeField] private Image nonActiveImage;
    [SerializeField] private IconState _iconState;
    [SerializeField] private Image _frameImage;
    [SerializeField] private Image _lightingFrameImage;
    [SerializeField] private TextMeshProUGUI _lvlText;

    [SerializeField] private Button _button;
    
    private TalentData _talent;
    private int _row = 0;


    public int Row => _row;

    public Button Button { get => _button; }
    public TalentData Talent => _talent;

    private void Start()
    {
       // _button.onClick.AddListener(Select);
    }

    public void SetActive()
    {
		//_button.onClick.AddListener(Select);
	}

    private void OnDestroy()
    {
        //_button.onClick.RemoveListener(Select);
    }

    public void Fill(TalentData talent, int row, bool isInteractable)
    {
        _button.interactable = isInteractable;
        _row = row;
        activeImage.sprite = talent.Icon;
        nonActiveImage.sprite = talent.Icon;
        _talent = talent;
        
        activeState.isActive = _talent.IsOpen;
        _lvlText.text = (talent.Level).ToString();
        _lvlText.gameObject.SetActive(_talent.IsOpen);
        if (_talent.IsOpen == false)
            _frameImage.sprite = _iconState.Off;
        else
            _frameImage.sprite = _iconState.On;
    }
    
    public void Select()
    {
        /*if (_talent.IsOpen)
        {
            if (_talent.Level < _talent.MaxLvl)
            {
                _lvlText.text = (_talent.Level + 1).ToString();
                Selected?.Invoke(_talent, _talent.IsOpen, _talent.Level+1);
                _lvlText.gameObject.SetActive(true);
            }
            else
            {
                Selected?.Invoke(_talent, !_talent.IsOpen, 0);
                _lvlText.text = "0";
                _lvlText.gameObject.SetActive(false);
            }
        }
        else
        {
            if (_talent.condition.CanOpen)
            {
                Selected?.Invoke(_talent, !_talent.IsOpen, 1);
                _lvlText.text = "1";
                _lvlText.gameObject.SetActive(true);
            }
        }
        activeState.isActive = _talent.IsOpen;*/
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEntered?.Invoke(_talent);
        _frameImage.sprite = _iconState.On;
        _lightingFrameImage.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExited?.Invoke(_talent);

        _lightingFrameImage.gameObject.SetActive(false);

        if (_talent.IsOpen == false)
            _frameImage.sprite = _iconState.Off;
    }

    private void OnLeftClick()
    {
        if (_talent.IsOpen)
        {
            if (_talent.Level < _talent.MaxLvl)
            {
                _lvlText.text = (_talent.Level + 1).ToString();
                Selected?.Invoke(_talent, true, _talent.Level + 1);
                _lvlText.gameObject.SetActive(true);
            }
           /* else
            {
                
                if (!_talent.CanClose())
                {
                    Debug.Log("CANT CLOSE TALENT", this);
                    return;
                }
                Selected?.Invoke(_talent, !_talent.IsOpen, 0);
                _lvlText.text = "0";
                _lvlText.gameObject.SetActive(false);
            }*/
        }
        else
        {
            if (_talent.condition.CanOpen)
            {
                Selected?.Invoke(_talent, true, 1);
                _lvlText.text = "1";
                _lvlText.gameObject.SetActive(true);
            }
        }
        activeState.isActive = _talent.IsOpen;
    }

    private void OnRightClick()
    {
        if(_talent.Level >= 2)
        {
            _lvlText.text = (_talent.Level - 1).ToString();
            Selected?.Invoke(_talent, true, _talent.Level - 1);
            _lvlText.gameObject.SetActive(true);

            return;
        }
        if (!_talent.CanClose())
        {
            Debug.Log("CANT CLOSE TALENT", this);
            return;
        }

        Selected?.Invoke(_talent, false, 0);
        _lvlText.text = "0";
        _lvlText.gameObject.SetActive(false);

        activeState.isActive = _talent.IsOpen;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick();
        }
    }
}
