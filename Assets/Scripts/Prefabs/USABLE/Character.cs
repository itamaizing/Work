using JetBrains.Annotations;
using UnityEngine;
using Mirror;
using UnityEngine.AI;


[RequireComponent(typeof(NetworkIdentity))]
public abstract class Character : NetworkBehaviour
{
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] private HealthComponent _healthComponent;
	[SerializeField] private MoveComponent _playerMove; 
	[SerializeField] private StaminaComponent _stamina;
	[CanBeNull] [SerializeField] private RuneComponent _runeComponent;
	[SerializeField] private PlayerAbilities _abilities;
	[SerializeField] private CharacterState _characterState;
	[SerializeField] private UIPlayerComponents _uiPlayerComponents;
	[SerializeField] private SelectComponent _selectComponent;
	[CanBeNull] [SerializeField] private SpawnComponent _spawnComponent;

	public Rigidbody2D Rb => _rb;
	public HealthComponent Health => _healthComponent;
	public MoveComponent Move => _playerMove;
	public StaminaComponent Stamina => _stamina;
	public RuneComponent RuneComponent => _runeComponent;
	public PlayerAbilities Abilities => _abilities;
	public CharacterState CharacterState => _characterState;
	public UIPlayerComponents UIPlayerComponents => _uiPlayerComponents;
	public SelectComponent SelectComponent => _selectComponent;
	public SpawnComponent SpawnComponent => _spawnComponent;

	public abstract void Initialize(CharacterData data);

	private void OnDestroy()
	{
		SelectManager.Instance.Deselect(this);
	}
}
