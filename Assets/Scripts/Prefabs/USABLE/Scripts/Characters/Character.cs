using System;
using JetBrains.Annotations;
using UnityEngine;
using Mirror;
using Pathfinding;
using Pathfinding.RVO;
using UnityEngine.Serialization;


[RequireComponent(typeof(NetworkIdentity))]
public abstract class Character : NetworkBehaviour
{
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] private AIPath _agent;
	[SerializeField] private RVOController _rvoController;
	[SerializeField] private HealthComponent _healthComponent;
	[SerializeField] private MoveComponent _playerMove; 
	[SerializeField] private StaminaComponent _stamina;
	[CanBeNull] [SerializeField] private RuneComponent _runeComponent;
	[SerializeField] private PlayerAbilities _abilities;
	[SerializeField] private CharacterState _characterState; 
	[SerializeField] private UIComponent uiComponent;
	[SerializeField] private SelectComponent _selectComponent;
	[CanBeNull] [SerializeField] private SpawnComponent _spawnComponent;
	[SerializeField] private TalentSystem _talentSystem;
	
	private TeamType _teamType;

	public Rigidbody2D Rb => _rb;
	protected AIPath Agent => _agent;
	protected RVOController RvoAgent => _rvoController;
	public HealthComponent Health => _healthComponent;
	public MoveComponent Move => _playerMove;
	public StaminaComponent Stamina => _stamina;
	public RuneComponent RuneComponent => _runeComponent;
	public PlayerAbilities Abilities => _abilities;
	public CharacterState CharacterState => _characterState;
	protected UIComponent UIComponent => uiComponent;
	public SelectComponent SelectComponent => _selectComponent;
	public SpawnComponent SpawnComponent => _spawnComponent;
	public TalentSystem TalentSystem => _talentSystem;

	public TeamType Team => _teamType;
	
	public static event Action<Character> ServerOnUnitSpawned;
	public static event Action<Character> ServerOnUnitDeleted; 
	
	public static event Action<Character> AuthorityOnUnitSpawned;
	public static event Action<Character> AuthorityOnUnitDeleted; 

	public abstract void Initialize(CharacterData data);
    
	
	public override void OnStartServer()
	{
		ServerOnUnitSpawned?.Invoke(this);
	}

	public override void OnStopServer()
	{
        
		ServerOnUnitDeleted?.Invoke(this);
	}

	public override void OnStartClient()
	{
		if (!isClientOnly && !isOwned)
		{
			return;
		}
		AuthorityOnUnitSpawned?.Invoke(this);
	}

	public override void OnStopClient()
	{
		if (!isClientOnly && !isOwned)
		{
			return;
		}
		AuthorityOnUnitDeleted?.Invoke(this);
	}
}
