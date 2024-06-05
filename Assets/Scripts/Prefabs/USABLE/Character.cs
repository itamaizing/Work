using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class Character : MonoBehaviour
{
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] private HealthComponent healthComponent;
	[SerializeField] private MoveComponent _playerMove; 
	[FormerlySerializedAs("staminaComponent")] [SerializeField] private StaminaComponent stamina;
	[CanBeNull]
	[SerializeField] private RuneComponent runeComponent;
	[SerializeField] private PlayerAbilities _abilities;
	[SerializeField] private CharacterState _characterState;
	[SerializeField] private UIPlayerComponents _uiPlayerComponents;
	[FormerlySerializedAs("_selectComponent")] [SerializeField] private SelectComponent selectComponentComponent;

	public Rigidbody2D Rb => _rb;
	public HealthComponent Health => healthComponent;
	public MoveComponent Move => _playerMove;
	public StaminaComponent Stamina => stamina;
	public RuneComponent RuneComponent => runeComponent;
	public PlayerAbilities Abilities => _abilities;
	public CharacterState CharacterState => _characterState;
	public UIPlayerComponents UIPlayerComponents => _uiPlayerComponents;
	public SelectComponent SelectComponentComponent => selectComponentComponent;

	public abstract void Initialize(CharacterData data);
}
