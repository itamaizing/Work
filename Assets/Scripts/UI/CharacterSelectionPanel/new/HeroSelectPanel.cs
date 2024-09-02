using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroSelectPanel : MonoBehaviour
{
    [SerializeField] private List<HeroComponent> _heroList;
    [SerializeField] private PlayerSelectionIcon _iconPref;

    private HeroComponent _selectedHero;
    private List<PlayerSelectionIcon> _iconList = new List<PlayerSelectionIcon>();

    public HeroComponent SelectedHero { get => _selectedHero; }


}
