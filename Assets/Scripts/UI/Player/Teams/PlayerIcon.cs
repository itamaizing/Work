using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerIcon : MonoBehaviour
{
    [SerializeField] private SelectManager _selectManager;
    [SerializeField] private GameObject _icon;
    [SerializeField] private Image _playerIcon;
    [SerializeField] private Bar _playerHp;
    [SerializeField] private Bar _playerMana;

    public void Init(Character character)
    {
        _playerIcon.sprite = character.Data.Icon;
        _playerHp.Init(character.Health);
        _playerMana.Init(character.Resources.FirstOrDefault(o=>o.Type == ResourceType.Mana));
    }

    private void Start()
    {
        if(_selectManager != null)
        {
            _selectManager.CharacterSelected += OnCharacterSelected;
            _selectManager.CharacterDeselected += OnCharacterDeselected;
        }
    }

    private void OnDestroy()
    {
        if (_selectManager != null)
        {
            _selectManager.CharacterSelected -= OnCharacterSelected;
            _selectManager.CharacterDeselected -= OnCharacterDeselected;
        }
    }

    private void OnCharacterSelected(Character character)
    {
        _icon.SetActive(true);

        _playerIcon.sprite = character.Data.Icon;
        _playerHp.Init(character.Health);
        _playerMana.Init(character.Resources.FirstOrDefault(o=>o.Type == ResourceType.Mana));
    }

    private void OnCharacterDeselected(Character character)
    {
        _icon.SetActive(false);
    }
}
