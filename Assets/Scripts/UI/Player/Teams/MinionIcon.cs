using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MinionIcon : MonoBehaviour
{
    [SerializeField] private Image _playerIcon;
    [SerializeField] private Bar _playerHp;
    [SerializeField] private Bar _playerMana;

    public void Init(Character character)
    {
        _playerIcon.sprite = character.Data.Icon;
        _playerHp.Init(character.Health);
        
        if (character.Resources.TryGetValue(ResourceType.Mana, out var mana))
        {
            _playerMana.Init(mana);
        }
    }
}
