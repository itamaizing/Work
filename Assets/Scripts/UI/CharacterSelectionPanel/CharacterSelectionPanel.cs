using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionPanel : MonoBehaviour
{
    [SerializeField] private MultiplayerManager _multiplayerManager;
    [SerializeField] private GridLayoutGroup _characterList;
    [SerializeField] private PlayerSelectionIcon _iconPref;

    private List<HeroComponent> _heroList;
    private HeroComponent _selectedHero;
    private List<PlayerSelectionIcon> _iconList = new List<PlayerSelectionIcon>();

    private void Start()
    {
        _heroList = _multiplayerManager.HeroList;

        foreach (var item in _heroList)
        {
            var icon = Instantiate(_iconPref, _characterList.transform);
            icon.Init(item);
            icon.Selected += OnPlayerSelected;
        }
        _selectedHero = _heroList[0];
    }

    private void OnPlayerSelected(HeroComponent hero)
    {
        _selectedHero = hero;
        _multiplayerManager.SetPlayer(_heroList.IndexOf(_selectedHero));
    }
}
