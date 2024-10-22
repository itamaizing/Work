using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
public abstract class Character : NetworkBehaviour
{
	[SerializeField] private CharacterData _playerData;
	[SerializeField] private UserNetworkSettings _networkSettings; 
	[SerializeField] private Rigidbody2D rb;
	[SerializeField] private Collider2D _collider; 
	[SerializeField] private Level _lvl;
	[SerializeField] private Health _healthComponent;
	[SerializeField] private MoveComponent _playerMove; 
	[SerializeField] private SkillManager _abilities;
	[SerializeField] private CharacterState _characterState;
	[SerializeField] private UIPlayerComponents uiComponent;
	[SerializeField] private SelectComponent _selectComponent;
	[SerializeField] private DamageTracker _damageTracker;
	[SerializeField] private List<Resource> _resources;
	[SerializeField] private SelectedCircle _selectedCircle;

    private bool _isInvisible = false;

	[SerializeField] private SpawnComponent _spawnComponent;

	public SpawnComponent SpawnComponent => _spawnComponent;
	public CharacterData Data => _playerData;
	public UserNetworkSettings NetworkSettings => _networkSettings;
	public Rigidbody2D Rb => rb;
	public Collider2D Collider => _collider;
	public Health Health => _healthComponent;
	public Level LVL => _lvl; 
	public MoveComponent Move => _playerMove;
	public SkillManager Abilities => _abilities;
	public CharacterState CharacterState => _characterState;
	public UIPlayerComponents UIComponent => uiComponent;
	public SelectComponent SelectComponent => _selectComponent;
	public DamageTracker DamageTracker => _damageTracker;
	public List<Resource> Resources => _resources;
    public SelectedCircle SelectedCircle => _selectedCircle;
    public bool IsInvisible 
	{ 
		get => _isInvisible; 
		
		set 
		{ 
			_isInvisible = value; 

			if (_isInvisible)
            {
                OnDisappeared?.Invoke();
            }
			else
			{
                OnAppeared?.Invoke();
            }
		} 
	}

    public static event Action<Character> ServerOnUnitSpawned;
	public static event Action<Character> ServerOnUnitDeleted; 
	public static event Action<Character> AuthorityOnUnitSpawned;
	public static event Action<Character> AuthorityOnUnitDeleted;
    public event Action OnDisappeared;
    public event Action OnAppeared;

    public virtual void Initialize()
	{
		Move.Initialize(Data.GetAttributeValue(AttributeNames.Speed), Rb , true);
		CharacterState.Initialize(this);
		SelectComponent.Initialize(Move,Abilities,UIComponent);
		
		foreach (var resource in Resources)
		{
			if (resource.Type == ResourceType.Health)
			{
				resource.Initialize(
					 Data.GetAttributeValue(AttributeNames.Health), 
					Data.GetAttributeValue(AttributeNames.HpRegen), 
					Data.GetAttributeValue(AttributeNames.HpRegenDelay), 
					Data);
			}
			if (resource.Type == ResourceType.Energy)
			{
				resource.Initialize(
					 Data.GetAttributeValue(AttributeNames.Energy), 
					Data.GetAttributeValue(AttributeNames.EnergyRegen), 
					Data.GetAttributeValue(AttributeNames.EnergyRegenDelay), 
					Data);
			}
			if (resource.Type == ResourceType.Mana)
			{
				resource.Initialize(
					 Data.GetAttributeValue(AttributeNames.Mana), 
					Data.GetAttributeValue(AttributeNames.ManaRegen), 
					Data.GetAttributeValue(AttributeNames.ManaRegenDelay), 
					Data);
			}
			if (resource.Type == ResourceType.Rune)
			{
				resource.Initialize(
					 Data.GetAttributeValue(AttributeNames.Rune), 
					Data.GetAttributeValue(AttributeNames.RuneRegen), 
					Data.GetAttributeValue(AttributeNames.RuneRegenDelay), 
					Data);
			}
		}
	}
	
	private void Start()
	{
		Initialize();
	}
	
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

    public Resource TryGetResource(ResourceType type)
	{
		return Resources.FirstOrDefault(r => r.Type == type);
	}
}
