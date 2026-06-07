using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TeamsPanel : MonoBehaviour
{
    public static TeamsPanel Instance;
    public event Action<Character> onPlayerSelected; 

    [SerializeField] private SelectManager _selectManager;
    [SerializeField] private PlayerIcon _playerIconPref;
    [SerializeField] private Image _team1;
    [SerializeField] private Image _team2;

    private List<PlayerIcon> _playerIcons = new();


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        if (_selectManager != null)
            _selectManager.OnListUpdated += UpdateTeams;

        StartCoroutine(UpdatePanelCurotine());
    }

    private void OnDisable()
    {
        if(_selectManager != null)
            _selectManager.OnListUpdated -= UpdateTeams;
    }

    private void UpdateTeams()
    {
        if (_selectManager.Characters.Count > _playerIcons.Count)
        {
            foreach (var character in _selectManager.Characters)
            {
                if (_playerIcons.FirstOrDefault(a => a.Character == character) != null) continue;
                character.NetworkSettings.OnUpdateValue += UpdatePanel;
                if (character.NetworkSettings.TeamIndex == 1)
                {
                    AddInFirstTeam(character);
                }
                else
                {
                    AddInSecondTeam(character);
                }
            }
        }
        else if(_selectManager.Characters.Count < _playerIcons.Count)
        {
            ClearPanel();
            FillPanel();
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

    public void AddInFirstTeam(Character character)
    {
        var icon = Instantiate(_playerIconPref, _team1.transform);
        icon.Init(character);
        _playerIcons.Add(icon);
    }

    public void AddInSecondTeam(Character character)
    {
        var icon = Instantiate(_playerIconPref, _team2.transform);
        icon.Init(character);
        _playerIcons.Add(icon);
    }

    public void StartReviveTimer(Character character, float time)
    {
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
    }

    private IEnumerator UpdatePanelCurotine()
    {
        yield return new WaitForSeconds(2);
        UpdateTeams();

        StartCoroutine(UpdatePanelCurotine());
    }

    private void FillPanel()
    {
        foreach (var character in _selectManager.Characters)
        {
            if (character.NetworkSettings.TeamIndex == 1)
            {
                AddInFirstTeam(character);
            }
            else
            {
                AddInSecondTeam(character);
            }
        }
    }

    private void UpdatePanel()
    {
        ClearPanel();
        FillPanel();
    }
}
