using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private HeroSpawnManager _heroSpawnManager;
    [SerializeField] private SourceUI _sourceUI;
    [SerializeField] private TeamsPanel _teamsPanel;
    [SerializeField] private TeamSource _sourceTabl;

    public HeroSpawnManager HeroSpawnManager { get => _heroSpawnManager; }
    public SourceUI SourceUI { get => _sourceUI; }
    public TeamsPanel TeamsPanel { get => _teamsPanel; }
    public TeamSource Source { get => _sourceTabl; }
}
