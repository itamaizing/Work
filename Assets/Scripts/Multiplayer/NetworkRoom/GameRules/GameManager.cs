using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;

public class GameManager : MonoBehaviour
{
    [SerializeField] private HeroSpawnManager _heroSpawnManager;
    [SerializeField] private PreparationAreaManager preparationAreaManager;
    [SerializeField] private NpcSpawn _npcSpawn;
    [SerializeField] private RestartRound restartRound;
    [SerializeField] private MonoBehaviour _sourceUI;
    [SerializeField] private TeamsPanel _teamsPanel;
    [SerializeField] private TeamSource _sourceTabl;
    [SerializeField] private ChatController _chatController;
    [SerializeField] private TMP_Text _teamKillScore;

    private IGameSourceUI _gameSourceUI;

    private void OnValidate()
    {
        if (_sourceUI == null)
            return;

        if (_sourceUI is IGameSourceUI ui)
        {
            _gameSourceUI = ui;
        }
        else
        {
            Debug.LogError("SOURCEUI WRONG");
            _sourceUI = null;
        }
    }
    
    public void SetTeamKillScore(int team1Kills, int team2Kills)
    {
        if (_teamKillScore != null)
            _teamKillScore.text = $"{team1Kills} - {team2Kills}";
    }

    public HeroSpawnManager HeroSpawnManager { get => _heroSpawnManager; }
    public PreparationAreaManager PreparationAreaManager { get => preparationAreaManager; }
    public IGameSourceUI SourceUI { get => _gameSourceUI; }
    public TeamsPanel TeamsPanel { get => _teamsPanel; }  
    public TeamSource Source { get => _sourceTabl; }
    public RestartRound RestartRound { get => restartRound; }
    public NpcSpawn NpcSpawn { get => _npcSpawn; }
    public ChatController ChatController { get => _chatController; }
}
