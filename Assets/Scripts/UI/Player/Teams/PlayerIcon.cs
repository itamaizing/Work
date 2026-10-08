using UnityEngine;
using UnityEngine.UI;

public class PlayerIcon : MonoBehaviour
{
    [SerializeField] private GameObject _icon;
    [SerializeField] private Image _playerIcon;
    [SerializeField] private ReviveVisualUI _reviveVisual;
    [SerializeField] private Bar _playerHp;
    [SerializeField] private Bar _playerMana;
    [SerializeField] private GameObject _iconHolder;
    
    private Character _character;
    private TeamsPanel _panel;

    public GameObject IconHolder => _iconHolder;
    public Character Character { get => _character; }

    public void Init(Character character, TeamsPanel panel)
    {
        _character = character;
        _panel = panel;
        if (_character == null)
        {
            _iconHolder.SetActive(false);
            return;
        }
        _iconHolder.SetActive(true);
        UpdateInfo(character);
    }

    public void OnCharacterSelected(Character character)
    {
        if (character == null)
        {
            _iconHolder.SetActive(false);
            return;
        }

        _iconHolder.SetActive(true);
        UpdateInfo(character);
    }
    public void OnCharacterDeselected(Character character)
    {
        _iconHolder.SetActive(false);
    }

    public void StartReviveTimer(float time)
    {
        _reviveVisual.StartTimer(time);
    }

    public void OnButtonClick()
    {
        _panel.OnButtonClick(_character);
    }

    protected virtual void UpdateInfo(Character character)
    {
        _playerIcon.sprite = character.Data.Icon;
        _playerHp.Init(character.Health);
        // _playerMana.Init(character.Resources.FirstOrDefault(o=>o.Type == ResourceType.Mana));
    }

}
