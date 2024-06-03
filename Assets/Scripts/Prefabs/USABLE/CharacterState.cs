using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Интерфейс состояния
public abstract class AbstractCharacterState
{
	public StateType type;
	public List<StatusEffect> effects = new List<StatusEffect>();
	protected CharacterState _characterState;
	public abstract void EnterState(CharacterState character, float durationToExit, float damageToExit);
	public abstract void UpdateState();
	public abstract void ExitState();
	public abstract bool Stack(float time);
}

public class DefaultState : AbstractCharacterState
{
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{

	}

	public override void UpdateState()
	{

	}

	public override void ExitState()
	{

	}

	public override bool Stack(float time)
	{
		return false;
	}
}


// Cостояние невидимость
public class InvisibleState : AbstractCharacterState
{
	private Renderer[] childRenderers;
	private SelectObject _select;
	private GameObject _player;

	private List<GameObject> _enemies = new List<GameObject>();

	private float lastCheckTime;
	private float checkInterval = 1f;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{
		Debug.Log("Entering Invisible State");
		_characterState = character;
		_select = character.Select;
		_player = character.gameObject;
	}

	public override void UpdateState()
	{
		Debug.Log("Updating Invisible State");

		childRenderers = _characterState.GetComponentsInChildren<Renderer>();

		if (_select.SelectedObject.CompareTag("Enemies") && _characterState.gameObject.CompareTag("Allies") ||
			_select.SelectedObject.CompareTag("Allies") && _characterState.gameObject.CompareTag("Enemies"))
		{

			// Выключаем видимость каждого дочернего Renderer
			foreach (Renderer renderer in childRenderers)
			{
				if (renderer != null)
				{
					renderer.enabled = false;
				}
			}
		}
		else
		{
			foreach (Renderer renderer in childRenderers)
			{
				if (renderer != null)
				{
					renderer.enabled = true;
				}
			}
		}

		if (_player.GetComponent<PlayerMove>().IsMoving)
		{
			CheckEnemies();
			//Раз в секунду проверяем дистанцию и шанс быть увиденным
			if (_enemies.Count > 0 && Time.time - lastCheckTime >= checkInterval)
			{
				CheckDistance();
				lastCheckTime = Time.time;
			}
		}
	}

	private void CheckEnemies()
	{
		int otherPlayersLayer = LayerMask.NameToLayer("OtherPlayers");
		string enemiesTag = "Enemies";
		float radius = 3f * 1.94f;

		Collider2D[] colliders = Physics2D.OverlapCircleAll(_player.transform.position, radius, 1 << otherPlayersLayer);

		foreach (Collider2D collider in colliders)
		{
			if (collider.CompareTag(enemiesTag))
			{
				//Направление врага
				Vector2 enemyMovementDirection = collider.GetComponent<PlayerMove>().DirectionOfMovement * radius;

				// Вектор от врага до плеера
				Vector2 playerToEnemy = _player.transform.position - collider.transform.position;

				// Проверяем, находится ли игрок спереди врага
				float dotProduct = Vector3.Dot(playerToEnemy.normalized, enemyMovementDirection);

				if (dotProduct > 0)
				{
					_enemies.Add(collider.gameObject);
				}
			}
		}
	}

	private void CheckDistance()
	{
		foreach (GameObject enemy in _enemies)
		{
			Vector2 enemyMovementDirection = enemy.GetComponent<PlayerMove>().DirectionOfMovement;
			Vector2 playerToEnemy = _player.transform.position - enemy.transform.position;

			// Находим перпендикулярный вектор к вектору направления врага и его длину
			Vector2 perpendicularVector = Vector3.ProjectOnPlane(playerToEnemy, enemyMovementDirection);
			float perpendicularDistance = perpendicularVector.magnitude;

			// Находим проекцию вектора playerToEnemy на вектор направления врага и ее длину
			float projection = Vector2.Dot(playerToEnemy, enemyMovementDirection);
			float projectionLength = Mathf.Abs(projection);

			float chanceToBeSeen = 0;

			if (projectionLength <= 1.94f * 1.5f)
			{
				if (perpendicularDistance <= 1.94f * 0.5f)
				{
					chanceToBeSeen = 0.8f;
				}
				else if (perpendicularDistance <= 1.94f * 1.5f && perpendicularDistance > 1.94f * 0.5f)
				{
					chanceToBeSeen = 0.7f;
				}
			}
			else if (projectionLength <= 1.94f * 2.5f && projectionLength > 1.94f * 1.5f)
			{
				if (perpendicularDistance <= 1.94f * 0.5f)
				{
					chanceToBeSeen = 0.3f;
				}
				else if (perpendicularDistance <= 1.94f * 1.5f && perpendicularDistance > 1.94f * 0.5f)
				{
					chanceToBeSeen = 0.2f;
				}
			}

			if (chanceToBeSeen > 0)
			{
				if (Random.value <= chanceToBeSeen)
				{
					_player.GetComponent<CharacterState>().AddState(new DefaultState(), States.Default);
				}
			}
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Invisible State");
		// При выходе из состояния возвращаем видимость дочерним Renderer
		if (childRenderers != null)
		{
			foreach (Renderer renderer in childRenderers)
			{
				if (renderer != null)
				{
					renderer.enabled = true;
				}
			}
		}
	}
	public override bool Stack(float time)
	{
		return false;
	}
}


// Cостояние оглушение
public class StunnedState : AbstractCharacterState
{
	public bool turnOff = false;
	private PlayerMove _playerMove;
	private PlayerAbilities _abilities;
	private float _baseDuration;
	private float _duration;
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{
		type = StateType.Physical;
		effects.Add(StatusEffect.Move);
		effects.Add(StatusEffect.Ability);

		Debug.Log("Entering Stunned State");
		_characterState = character;
		_abilities = character.PlayerLinks.Abilities;
		_abilities.SetAbilitiesDisabled();
		_playerMove.CanMove = false;
		_duration = durationToExit;
		_baseDuration = durationToExit;
		//_duration = character.durationToExit;      
	}

	public override void UpdateState()
	{
		Debug.Log("Updating Stunned State");
		_duration -= Time.deltaTime;
		if (_duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Stunned State");
		if (_characterState.Check(StatusEffect.Move))
		{
			_playerMove.CanMove = true;
		}
		if (_characterState.Check(StatusEffect.Ability))
		{
			_abilities.SetAbilitiesEnabled();
		}
		_characterState.RemoveState(this);
	}
	public override bool Stack(float time)
	{
		if (_baseDuration > time)
		{
			return false;
		}
		else
		{
			_duration = time;
			return true;
		}
	}
}

// Cостояние ослепление
public class BlindnessState : AbstractCharacterState
{
	public bool turnOff = false;

	//private CharacterState _characterState;
	private float _duration;
	private float _baseDuration;
	private PlayerAbilities _abilities;
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{
		type = StateType.Physical;
		effects.Add(StatusEffect.Ability);
		Debug.Log("Entering Stunned State");
		_duration = durationToExit;
		_baseDuration = durationToExit;
		_characterState = character;
		if (character.PlayerLinks.Abilities != null)
		{
			_abilities = character.PlayerLinks.Abilities;
			_abilities.SetAbilitiesDisabled();
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}
	}

	public override void UpdateState()
	{
		Debug.Log("Updating Stunned State");
		_duration -= Time.deltaTime;
		if (_duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Stunned State");
		if (_characterState.Check(StatusEffect.Ability))
		{
			_abilities.SetAbilitiesEnabled();
		}
		_characterState.RemoveState(this);
	}
	public override bool Stack(float time)
	{
		if (_baseDuration > time)
		{
			return false;
		}
		else
		{
			_duration = time;
			return true;
		}

	}
}

// Cостояние заморозки
public class FrozenState : AbstractCharacterState
{
	public bool turnOff = false;

	//private CharacterState _characterState;
	private HealthPlayer _playerHP;
	private PlayerMove _playerMove;
	private float _duration;
	private float _baseDuration;
	private float _damageToExit;
	private PlayerAbilities _abilities;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{
		type = StateType.Magic;
		effects.Add(StatusEffect.Move);
		effects.Add(StatusEffect.Ability);
		Debug.Log("Entering Frozen State");
		_characterState = character;
		_duration = durationToExit;
		if (damageToExit == 0)
		{
			_damageToExit = 10000;
		}
		else
		{
			_damageToExit = damageToExit;
		}
		_playerHP = _characterState.PlayerLinks.HealthPlayer;

		_playerMove = _characterState.PlayerLinks.PlayerMove;
		_playerMove.CanMove = false;

		if (character.PlayerLinks.Abilities != null)
		{
			_abilities = character.PlayerLinks.Abilities;
			_abilities.SetAbilitiesDisabled();
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}
		//_playerHP.TakePhisicDamage(10 + _characterState.energy.Value / 4);
		//_playerHP.TakeDamage(10 + _characterState.energy.Value / 4, DamageType.Physical);
		//_duration = 2 + _characterState.energy.Value / 20; //тут мана того кто стрелял
		//_characterState.energy.Use(_characterState.energy.Value);
		_playerHP.sumDamageTaken = 0;

	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_playerHP.sumDamageTaken >= _damageToExit || _duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Frozen State");

		//character.GetAbilityManager().ToggleAbility(true);//turn on abilities
		if (_characterState.Check(StatusEffect.Move))
		{
			_playerMove.CanMove = true;
		}
		if (_characterState.Check(StatusEffect.Ability))
		{
			_abilities.SetAbilitiesEnabled();
		}
		_characterState.RemoveState(this);
	}
	public override bool Stack(float time)
	{
		_duration = _baseDuration;
		return false;
	}
}

//change to ability speed drop
public class FrostingState : AbstractCharacterState
{
	public bool turnOff = false;

	private HealthPlayer _playerHP;
	private PlayerMove _targetMove;
	private float _duration;
	private float _baseDuration;
	private float _damageToExit;
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{
		type = StateType.Magic;
		effects.Add(StatusEffect.Move);
		effects.Add(StatusEffect.AbilitySpeed);
		Debug.Log("Entering Frosting State");
		_characterState = character;
		_targetMove = _characterState.PlayerLinks.PlayerMove;
		if (damageToExit == 0)
		{
			_damageToExit = 10000;
		}
		else
		{
			_damageToExit = damageToExit;
		}
		_duration = durationToExit;
		_baseDuration = durationToExit;
		_playerHP = _characterState.PlayerLinks.HealthPlayer;

		_targetMove.CanMove = false;
		//decrease speed of attact
		//_playerHP.TakePhisicDamage(10 + _characterState.energy.Value / 4);
		//_playerHP.TakeDamage(10 + _characterState.energy.Value / 4, DamageType.Physical);
		_playerHP.sumDamageTaken = 0;

		//_characterState.energy.Use(_characterState.energy.Value);
	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_playerHP.sumDamageTaken >= _damageToExit || _duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Frosting State");
		if (_characterState.Check(StatusEffect.Move))
		{
			_targetMove.CanMove = true;
		}
		if (_characterState.Check(StatusEffect.AbilitySpeed))
		{
			//return speed of attact
		}
		_characterState.RemoveState(this);
	}
	public override bool Stack(float time)
	{
		_duration = _baseDuration;
		return false;
	}
}

// Класс персонажа, использующий состояния
public class CharacterState : MonoBehaviour
{
	public PlayerLinks PlayerLinks => _links;
	public SelectObject Select;

	[SerializeField] private PlayerLinks _links;
	//[SerializeField] private StateIcons _icons;
	[SerializeField] private List<AbstractCharacterState> currentStates = new List<AbstractCharacterState>();

	private void Start()
	{
		if (Select == null || _links.HealthPlayer == null || _links.PlayerMove == null || _links == null)
		{
			Debug.LogError("No required component in " + gameObject.name);
		}
	}

	private void Update()
	{
		// Обновление текущего состояния
		if (currentStates.Count > 0)
		{
			for (int i = 0; i < currentStates.Count; i++)
			{
				currentStates[i].UpdateState();
			}
		}
	}

	public void AddState(AbstractCharacterState newState, States state)
	{
		// переделать под лист
		//if already has, reset???
		Debug.Log("THIS IS OLD SYSTEM TO ADD STATE, USE THIS AddState(AbstractCharacterState newState, float duration, float damageToExit, States state)");
		// Вход в новое состояние
		currentStates.Add(newState);
		currentStates[currentStates.Count - 1].EnterState(this, 0, 0);
	}
	public void AddState(AbstractCharacterState newState, float duration, float damageToExit, States state)
	{
		//if already exist 
		if (currentStates.Contains(newState))
		{
			foreach (AbstractCharacterState item in currentStates)
			{
				if (item.Stack(duration))
				{
					_links.StateIcons.ActivateIco(state, duration, 1);
				}
				else
				{
					//nothing at this time??
				}
			}
		}
		else
		{
			_links.StateIcons.ActivateIco(state, duration, 1);
			currentStates.Add(newState);
			//currentStates[currentStates.Count - 1].
			currentStates[currentStates.Count - 1].EnterState(this, duration, damageToExit);
		}
	}
	public bool IfHasState(AbstractCharacterState newState)
	{
		if (currentStates.Contains(newState))
		{
			return true;
		}
		else return false;
	}

	public void RemoveState(AbstractCharacterState newState)
	{
		//newState.ExitState(this);
		currentStates.Remove(newState);
	}

	public void Dispel(StateType type)
	{
		foreach (AbstractCharacterState state in currentStates)
		{
			if (state.type == type)
			{
				state.ExitState();
			}
		}
	}
	/* public AbilityManager GetAbilityManager()
     {
         if( _abilityManager == null ) 
         {
             Debug.LogError("No ability manager!");
             return null;
         }
         return _abilityManager;
     }*/

	public bool Check(StatusEffect effect)
	{
		foreach (AbstractCharacterState state in currentStates)
		{
			if (state.effects.Contains(effect))
			{
				return false;
			}
		}
		return true;
	}
}

public enum StateType
{
	Physical,
	Magic,
	Third
}

public enum StatusEffect
{
	Move,
	Ability,
	AbilitySpeed,
	Others
}
