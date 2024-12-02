using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using Org.BouncyCastle.Pqc.Crypto.Lms;

[RequireComponent(typeof(NetworkIdentity))]
public abstract class Character : NetworkBehaviour, IDamageable, IHealingable
{
	[SerializeField] private CharacterData _playerData;
	[SerializeField] private UserNetworkSettings _networkSettings; 
	[SerializeField] private Rigidbody _rigidbody;
	[SerializeField] private Collider _collider;
	[SerializeField] private Level _lvl;
	[SerializeField] private Animator _animator;
	[SerializeField] private NetworkAnimator _networkAnimator;
	[SerializeField] private Health _healthComponent;
	[SerializeField] private MoveComponent _playerMove; 
	[SerializeField] private SkillManager _abilities;
	[SerializeField] private CharacterState _characterState;
	[SerializeField] private UIPlayerComponents uiComponent;
	[SerializeField] private SelectComponent _selectComponent;
	[SerializeField] private DamageTracker _damageTracker;
	[SerializeField] private List<Resource> _resources;
	[SerializeField] private SelectedCircle _selectedCircle;
	[SerializeField] private SpawnComponent _spawnComponent;

	private bool _isDead = false;

	public SpawnComponent SpawnComponent => _spawnComponent;
	public CharacterData Data => _playerData;
	public UserNetworkSettings NetworkSettings => _networkSettings;
	public Rigidbody Rigidbody => _rigidbody;
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
    public Animator Animator => _animator;
    public NetworkAnimator NetworkAnimator => _networkAnimator;
    public bool IsDead => _isDead;

    public static event Action<Character> ServerOnUnitSpawned;
	public static event Action<Character> ServerOnUnitDeleted; 
	public static event Action<Character> AuthorityOnUnitSpawned;
	public static event Action<Character> AuthorityOnUnitDeleted;
    public event Action<Damage, Skill> DamageTaken;
    public event Action<float, Skill, string> HealTaked;
	public event Action<Character> Died;

    protected override void OnValidate()
    {
		base.OnValidate();

        if (_collider == null)
        {
			Debug.LogError("Fill in field Collider on prefab");
        }
    }

    public virtual void Initialize()
	{
		Move.Initialize(Data.GetAttributeValue(AttributeNames.Speed), Rigidbody , true);
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
		Health.Died += CmdOnDied;
	}
	
	private void Start()
	{
		Initialize();
	}

	[Server]
	public void ServerResetAll()
    {
		ResetAll();
		RpcResetAll();
	}

	private void ResetAll()
    {
		_isDead = false;
		_animator.SetTrigger(HashAnimPlayer.Revival);
		_collider.enabled = true;
		_rigidbody.isKinematic = false;
	}

	[ClientRpc]
	private void RpcResetAll()
    {
		ResetAll();
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

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
		return Health.TryTakeDamage(ref damage, skill);
    }

    public void ShowPhantomValue(Damage phantomValue)
    {
		Health.ShowPhantomValue(phantomValue);
	}

    public void Heal(ref Heal value, string sourceName, Skill skill)
    {
		Health.Heal(ref value, sourceName, skill);
	}

    private void OnDied()
    {
		Died?.Invoke(this);

		_isDead = true;
		_animator.SetTrigger(HashAnimPlayer.Die);
		_collider.enabled = false;
		_rigidbody.isKinematic = true;
		DeleteStates();
	}

	private void DeleteStates()
    {
		var statesCopy = new List<AbstractCharacterState>(_characterState.CurrentStates);
		foreach (var state in statesCopy)
		{
			_characterState.RemoveState(state.State);
		}
	}

	[Command]
	private void CmdOnDied()
    {
		OnDied();
		ClientRpcOnDied();
	}

	[ClientRpc]
	private void ClientRpcOnDied()
    {
		OnDied();
	}
}
