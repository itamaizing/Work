using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TeamsPanel : MonoBehaviour
{
    public event Action<Character> onPlayerSelected; 
    
    [SerializeField] private PlayerIcon _allyIconPrefab;
    [SerializeField] private PlayerIcon _enemyIconPrefab;
    [SerializeField] private PlayerIcon _localHeroIcon;
    [SerializeField] private RectTransform _allies;
    [SerializeField] private RectTransform _enemies;

    private List<PlayerIcon> _playerIcons = new();
    
    private Coroutine _refreshCoroutine;


    private void OnEnable()
    {
        _refreshCoroutine = StartCoroutine(RefreshTeams());
    }

    private void OnDisable()
    {
        if (_refreshCoroutine != null)
        {
            StopCoroutine(_refreshCoroutine);
            _refreshCoroutine = null;
        }
    }
    
    private IEnumerator RefreshTeams()
    {
        var delay = new WaitForSecondsRealtime(0.25f);

        while (true)
        {
            UpdateTeams();
            yield return delay;
        }
    }
    
    private void UpdateTeams()
    {
        var local = Character.Local;
        if (local == null || local.NetworkSettings == null ||
            local.NetworkSettings.TeamIndex == 0)
        {
            ClearPanel();
            return;
        }

        var localTeam = local.NetworkSettings.TeamIndex;
        var heroes = local.NetworkSettings.Players
            .Where(player => player != null)
            .Select(player => player.GetComponent<HeroComponent>())
            .Where(hero => hero != null && hero.IsInitialized && hero.NetworkSettings != null && hero.NetworkSettings.TeamIndex != 0)
            .Distinct()
            .ToList();

        for (int i = _playerIcons.Count - 1; i >= 0; i--)
        {
            var icon = _playerIcons[i];
            if (icon != null && icon.Character is HeroComponent hero && heroes.Contains(hero))
            {
                var expectedParent = hero.NetworkSettings.TeamIndex == localTeam ? _allies : _enemies;

                if (icon.transform.parent == expectedParent)
                    continue;
            }

            if (icon != null)
                Destroy(icon.gameObject);

            _playerIcons.RemoveAt(i);
        }

        foreach (var hero in heroes)
        {
            if (!_playerIcons.Any(icon => icon.Character == hero))
                AddHero(hero);
        }
    }

    private void ClearPanel()
    {
        foreach (PlayerIcon playerIcon in _playerIcons)
        {
            Destroy(playerIcon.gameObject);
        }
        _playerIcons.Clear();
    }

    public void AddInFirstTeam(Character character) => AddHero(character);
    public void AddInSecondTeam(Character character) => AddHero(character);
    
    private void AddHero(Character character)
    {
        if (character == null || character is not HeroComponent hero)
            return;

        var local = Character.Local;
        if (local == null || local.NetworkSettings == null || hero.NetworkSettings == null)
            return;

        int localTeam = local.NetworkSettings.TeamIndex;
        int heroTeam = hero.NetworkSettings.TeamIndex;
        if (localTeam == 0 || heroTeam == 0)
            return;

        bool isAlly = heroTeam == localTeam;
        var prefab = isAlly ? _allyIconPrefab : _enemyIconPrefab;
        var parent = isAlly ? _allies : _enemies;

        var icon = Instantiate(prefab, parent);
        icon.Init(hero, this);
        if (hero == local)
        {
            icon.GetComponent<LayoutElement>().preferredHeight *= 1.2f;
            icon.IconHolder.transform.localScale = Vector3.Scale(icon.transform.localScale, new Vector3(1.2f, 1.2f, 1f));
            icon.transform.SetAsFirstSibling();
        }
        _playerIcons.Add(icon);
    }

    public void StartReviveTimer(Character character, float time)
    {
        if (character != null && character == Character.Local && _localHeroIcon != null)
            _localHeroIcon.StartReviveTimer(time);

        foreach (var item in _playerIcons)
        {
            if (item.Character == character)
            {
                item.StartReviveTimer(time);
                break;
            }   
        }
    }

    public void OnButtonClick(Character character)
    {
        onPlayerSelected?.Invoke(character);
        Debug.Log("Player click " + character);
    }
}
