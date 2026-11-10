using UnityEngine;
using UnityEngine.UI;

public class MinionIcon : MonoBehaviour
{
    [SerializeField] private Image _playerIcon;
    [SerializeField] private HUDResourceBar _playerHp;
    [SerializeField] private Bar _playerMana;
    [SerializeField] private HUDStatePanel _buffs;
    [SerializeField] private HUDStatePanel _debuffs;

    public void Init(Character character)
    {
        _playerIcon.sprite = character.Data.Icon;
        _playerHp.Bind(character.Health);
        _buffs?.SetTarget(character);
        _debuffs?.SetTarget(character);
        
        if (character.Resources.TryGetValue(ResourceType.Mana, out var mana))
        {
            _playerMana.Init(mana);
        }
    }
}
