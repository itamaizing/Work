using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLinks : MonoBehaviour
{
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] private HealthPlayer _healthPlayer;
	[SerializeField] private PlayerMove _playerMove;
	[SerializeField] private PlayerStamina _playerStamina;
	[SerializeField] private RunePlayer _runePlayer;
	[SerializeField] private PlayerAbilities _abilities;
	[SerializeField] private CharacterState _characterState;
	[SerializeField] private StateIcons _stateIcons;
	//[SerializeField] private SelectObject _selectObject;

	public Rigidbody2D Rb => _rb;
	public HealthPlayer HealthPlayer => _healthPlayer;
	public PlayerMove PlayerMove => _playerMove;
	public PlayerStamina Stamina => _playerStamina;
	public RunePlayer RunePlayer => _runePlayer;
	public PlayerAbilities Abilities => _abilities;
	public CharacterState CharacterState => _characterState;
	public StateIcons StateIcons => _stateIcons;
	//public SelectObject SelectObject => _selectObject;
}
