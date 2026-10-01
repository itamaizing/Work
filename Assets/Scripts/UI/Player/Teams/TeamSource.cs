using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamSource : MonoBehaviour
{
    [SerializeField] private HeroInfoUI _heroInfoPrefab;
    [SerializeField] private RectTransform _teamTransformFirst;
    [SerializeField] private RectTransform _teamTransformSecond;
    
    private List<HeroInfoUI> _heroInfoList = new();

    private void OnEnable()
    {
        UpdateInfo();
    }

    public void AddInTeam(Character character)
    {
        HeroInfoUI _heroInfoUI = Instantiate(_heroInfoPrefab,character.NetworkSettings.TeamIndex == 1 ? _teamTransformFirst : _teamTransformSecond);
        _heroInfoUI.SetHero(character);
        _heroInfoList.Add(_heroInfoUI);
    }

    public void UpdateInfo()
    {
        foreach (var item in _heroInfoList)
        {
            item.UpdateInfo();
        }
    }
}
