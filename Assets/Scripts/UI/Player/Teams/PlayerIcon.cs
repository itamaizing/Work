using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerIcon : MonoBehaviour
{
    [SerializeField] private GameObject _icon;
    [SerializeField] private Image _playerIcon;
    [SerializeField] private Bar _playerHp;
    [SerializeField] private Bar _playerMana;

	public void Init(Character character)
    {
        UpdateInfo(character);
    }

    public void OnCharacterSelected(Character character)
    {
        _icon.SetActive(true);

        UpdateInfo(character);
    }

    public void OnCharacterDeselected(Character character)
    {
        _icon.SetActive(false);
    }

    protected virtual void UpdateInfo(Character character)
    {
        _playerIcon.sprite = character.Data.Icon;
        _playerHp.Init(character.Health);
        // _playerMana.Init(character.Resources.FirstOrDefault(o=>o.Type == ResourceType.Mana));
    }
}
