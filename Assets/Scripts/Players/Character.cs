using System;
using JetBrains.Annotations;
using UnityEngine;

public class Character : MonoBehaviour
{
	[SerializeField] private PlayerInfo _playerData;
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] private HealthPlayer _healthPlayer;
	[SerializeField] private PlayerMove _playerMove;
	[SerializeField] private PlayerStamina _playerStamina;
	[CanBeNull]
	[SerializeField] private RunePlayer _runePlayer;
	[SerializeField] private PlayerAbilities _abilities;
	[SerializeField] private CharacterState _characterState;
	[SerializeField] private UIPlayerComponents _uiPlayerComponents;

	public Rigidbody2D Rb => _rb;
	public HealthPlayer Health => _healthPlayer;
	public PlayerMove Move => _playerMove;
	public PlayerStamina Stamina => _playerStamina;
	public RunePlayer RunePlayer => _runePlayer;
	public PlayerAbilities Abilities => _abilities;
	public CharacterState CharacterState => _characterState;
	public UIPlayerComponents UIPlayerComponents => _uiPlayerComponents;

	private void Awake()
	{
		Initialize();
	}

	public void Initialize()
	{
		Health.Initialize(_playerData.Health,_playerData.HealthRegen,_playerData.RegenDelay ,_playerData.HealthInfo);
		Move.Initialize(_playerData.MoveSpeed,Rb);
		Stamina.Initialize(_playerData.Stamina, _playerData.StaminaRegen, _playerData.RegenDelay);
		RunePlayer.Initialize(10,3,10);
		CharacterState.Initialize(Health, Move);
		UIPlayerComponents.Initialize(Abilities, this);
	}
}
