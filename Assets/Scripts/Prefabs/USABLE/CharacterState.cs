using Mirror;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
public abstract class AbstractCharacterState
{
	protected CharacterState _characterState;
	protected SkillManager _abilities;
	protected Health _health;
	protected Character _personWhoMadeBuff;

	public abstract States State { get; }
	public abstract StateType Type { get; }
	public abstract List<StatusEffect> Effects { get; }

	public abstract void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName);
	public abstract void UpdateState();
	public abstract void ExitState();
	public abstract bool Stack(float time);
}

public class DefaultState : AbstractCharacterState
{
	private List<StatusEffect> _effects = new List<StatusEffect>();
	public override States State => States.Default;

	public override StateType Type => StateType.Physical;

	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
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

public class ImmaterialityState : AbstractCharacterState
{
	private float _duration;
	private float _baseDuration;
	private Character _player;

	private List<StatusEffect> _effects = new List<StatusEffect>();
	public override States State => States.Immateriality;

	public override StateType Type => StateType.Physical;

	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_characterState = character;
		_player = _characterState.Character;
		Debug.Log("enterState Immateriality");
		_duration = durationToExit;
		_baseDuration = _duration;

		DisabledCollider();
	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_duration <= 0)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		_player.Collider.enabled = true;
		_duration = 0;
		_baseDuration = 0;
		_characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		return false;
	}

	private void DisabledCollider()
	{
		if (_player != null)
		{
			Debug.Log("DisabledCollider if player != null / _player.collider == " + _player.Collider);
			_player.Collider.enabled = false;
		}
	}
}

public class InvisibleStateOld : AbstractCharacterState
{
	private Renderer[] childRenderers;
	private GameObject _player;

	private List<GameObject> _enemies = new List<GameObject>();

	private float lastCheckTime;
	private float checkInterval = 1f;
	private List<StatusEffect> _effects = new List<StatusEffect>();

	public override States State => States.Invisible;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		Debug.Log("Entering Invisible State");
		_characterState = character;
		_player = character.gameObject;
	}

	public override void UpdateState()
	{
		Debug.Log("Updating Invisible State");

		childRenderers = _characterState.GetComponentsInChildren<Renderer>();
/*
		if (_select.SelectedObject.CompareTag("Enemies") && _characterState.gameObject.CompareTag("Allies") ||
			_select.SelectedObject.CompareTag("Allies") && _characterState.gameObject.CompareTag("Enemies"))
		{

			// ��������� ��������� ������� ��������� Renderer
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
*/
		if (_characterState.Character.Move.IsMoving)
		{
			CheckEnemies();
			//��� � ������� ��������� ��������� � ���� ���� ���������
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
				//����������� �����
				Vector2 enemyMovementDirection = collider.GetComponent<MoveComponent>().MoveDirection * radius;

				// ������ �� ����� �� ������
				Vector2 playerToEnemy = _player.transform.position - collider.transform.position;

				// ���������, ��������� �� ����� ������� �����
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
			Vector2 enemyMovementDirection = enemy.GetComponent<MoveComponent>().MoveDirection;
			Vector2 playerToEnemy = _player.transform.position - enemy.transform.position;

			// ������� ���������������� ������ � ������� ����������� ����� � ��� �����
			Vector2 perpendicularVector = Vector3.ProjectOnPlane(playerToEnemy, enemyMovementDirection);
			float perpendicularDistance = perpendicularVector.magnitude;

			// ������� �������� ������� playerToEnemy �� ������ ����������� ����� � �� �����
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
					//_player.GetComponent<CharacterState>().AddState(new DefaultState(), States.Default);
					ExitState();
				}
			}
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Invisible State");
		// ��� ������ �� ��������� ���������� ��������� �������� Renderer
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

public class InvisibleState : AbstractCharacterState
{
	public bool turnOff = false;
	private float _baseDuration;
	private float _duration;
	private List<StatusEffect> _effects = new List<StatusEffect>();

	public override States State => States.Invisible;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		Debug.Log("Entering Invisible State");
		//effects.Add(StatusEffect.Others);

		_characterState = character;
		//_characterState.Health.SetInvincible(true);
		_characterState.invinsible = true;
		_duration = durationToExit;
		_baseDuration = durationToExit;
	}

	public override void UpdateState()
	{
		Debug.Log("Updating Invisible State");
		_duration -= Time.deltaTime;
		if (_duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Invisible State");
		if (_characterState.Check(StatusEffect.Others))
		{
			//_characterState.Health.SetInvincible(false);
			_characterState.invinsible = false;
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

public class StunnedState : AbstractCharacterState
{
	public bool turnOff = false;
	private float _baseDuration;
	private float _duration;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability};

	public override States State => States.Stun;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		Debug.Log("Entering Stunned State");

		_characterState = character;

		if (character.TryGetComponent<Character>(out var ability))
		{
			_abilities = ability.Abilities;
			_abilities.SetAbilitiesDisabled();
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}		
		_characterState.Character.Move.CanMove = false;
		_duration = durationToExit;
		_baseDuration = durationToExit;
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
			_characterState.Character.Move.CanMove = true;
		}
		if (_characterState.Check(StatusEffect.Ability) && _abilities != null)
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

public class Desiccuration : AbstractCharacterState
{
	public bool turnOff = false;
	private float _baseDuration;
	private float _duration;
	private float _damageToExit;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability};

	public override States State => States.Desiccuration;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		Debug.Log("Entering Desiccuration State");

		_characterState = character;

		if (character.TryGetComponent<Character>(out var ability))
		{
			_abilities = ability.Abilities;
			_abilities.SetAbilitiesDisabled();
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}
		_characterState.Character.Move.CanMove = false;
		_duration = durationToExit;
		_baseDuration = durationToExit;
		//_damageToExit = damageToExit;
		_damageToExit = 0.01f;
	}

	public override void UpdateState()
	{
		Debug.Log("Updating Desiccuration State");
		_duration -= Time.deltaTime;
		if (_duration < 0 || turnOff || _characterState.Character.Health.SumDamageTaken >= _damageToExit)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Desiccuration State");
		if (_characterState.Check(StatusEffect.Move))
		{
			_characterState.Character.Move.CanMove = true;
		}
		if (_characterState.Check(StatusEffect.Ability) && _abilities != null)
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

public class BlindnessState : AbstractCharacterState
{
	public bool turnOff = false;

	//private CharacterState _characterState;
	private float _duration;
	private float _baseDuration;

	private List<StatusEffect> _effects = new List<StatusEffect>() {StatusEffect.Ability};

	public override States State => States.Blind;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;

	//private PlayerAbilities _abilities;
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		Debug.Log("Entering Stunned State");
		_duration = durationToExit;
		_baseDuration = durationToExit;
		_characterState = character;
		if (character.TryGetComponent<Character>(out var ability))
		{
			_abilities = ability.Abilities;
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

public class FrozenState : AbstractCharacterState
{
	public bool turnOff = false;
	private float _duration;
	private float _baseDuration;
	private float _damageToExit;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability };

	public override States State => States.Frozen;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
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
		
		_characterState.Character.Move.CanMove = false;

		if (character.TryGetComponent<Character>(out var ability))
		{
			_abilities = ability.Abilities;
			_abilities.SetAbilitiesDisabled();
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}

		//_characterState.Health.sumDamageTaken = 0;

	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_characterState.Character.Health.SumDamageTaken >= _damageToExit || _duration < 0 || turnOff)
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
			_characterState.Character.Move.CanMove = true;
		}
		if (_characterState.Check(StatusEffect.Ability) && _abilities!=null)
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

public class FrostingState : AbstractCharacterState
{
	public bool turnOff = false;
	private float _duration;
	private float _baseDuration;
	private float _damageToExit;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

	public override States State => States.Frosting;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		Debug.Log("Entering Frosting State");
		_characterState = character;
		
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

		_characterState.Character.Move.CanMove = false;

		//decrease speed
		if (character.TryGetComponent<Character>(out var ability))
		{
			_abilities = ability.Abilities;

			foreach (Skill abil in _abilities.Abilities)
			{
				if (abil.AbilityForm == AbilityForm.Physical)
				{
					abil.Buff.CastSpeed.ReductionPercentage(.5f);
				}
			}
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}

		//_characterState.Health.sumDamageTaken=0;
	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_characterState.Character.Health.SumDamageTaken >= _damageToExit || _duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Frosting State");
		if (_characterState.Check(StatusEffect.Move))
		{
			_characterState.Character.Move.CanMove = true;
		}
		if (_characterState.Check(StatusEffect.AbilitySpeed))
		{
			foreach (Skill abil in _abilities.Abilities)
			{
				if (abil.AbilityForm == AbilityForm.Physical)
				{
					abil.Buff.CastSpeed.IncreasePercentage(.5f);
				}
			}
		}
		_characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		_duration = _baseDuration;
		return true;
	}

}

public class Cooling : AbstractCharacterState
{
	public bool turnOff = false;
	private float _duration;
	private float _baseDuration;
	private float _damageToExit;
	private float _curAbilityDebuf = 0.1f;
	private float _curSpeedDebuf = 0.05f;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.MoveSpeed, StatusEffect.AbilitySpeed};

	public override States State => States.Cooling;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		Debug.Log("Entering cooling State");
		_characterState = character;

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

		_characterState.Character.Move.ChangeMoveSpeed(1-_curSpeedDebuf);
		//decrease speed of attact and movement
		//_characterState.Health.sumDamageTaken = 0;
	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_characterState.Character.Health.SumDamageTaken >= _damageToExit || _duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting cooling State");
		if (_characterState.Check(StatusEffect.MoveSpeed))
		{
			_characterState.Character.Move.SetDefaultSpeed();
			//_characterState.Move.CanMove = true;
		}
		if (_characterState.Check(StatusEffect.AbilitySpeed))
		{
			//return speed of attact
		}
		_characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		Debug.Log("stacked");
		//_characterState.Move.SetDefaultSpeed();
		_duration = time;
		_curSpeedDebuf += 0.05f;
		_curAbilityDebuf += 0.1f;
		//ability speed decrease
		_characterState.Character.Move.ChangeMoveSpeed(1 - _curSpeedDebuf);
		//_duration = _baseDuration;
		return true;
	}

}

public class InAirState : AbstractCharacterState
{
    public bool turnOff = false;

    private float _duration;
    private float _baseDuration;
    private float _damageToExit;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };
    public override States State => States.InAir;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
		Debug.Log("InAirState / EnterState");
        _characterState = character;

        if (character.TryGetComponent<Character>(out var ability))
        {
            _abilities = ability.Abilities;
            _abilities.SetAbilitiesDisabled();
        }
        else
        {
            Debug.Log("no ability at " + character.gameObject.name);
        }

		_characterState.Character.Move.CanMove = false;
		_duration = durationToExit;
        _baseDuration = _duration;
        _baseDuration = durationToExit;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }
    }


    public override void ExitState()
    {
        if (_characterState.Check(StatusEffect.Move))
        {
            _characterState.Character.Move.CanMove = true;
        }
        if (_characterState.Check(StatusEffect.Ability) && _abilities != null)
        {
            _abilities.SetAbilitiesEnabled();
        }
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }
}

#region CreeperStates

public class CreeperInvisibleState : AbstractCharacterState
{
    public bool turnOff = false;

    private List<Skill> _skills = new();
	private CreeperInvisible _creeperInvisible;
    private Character _player;

	private float _reductionMoveSpeed = 0.3f;
	private float _originalMoveSpeed;
	private float _increaseStaminaRegen = 0.3f;
	private float _originalStaminaRegen;

	private static bool _isIncreasedManaCost = false;
	private bool _isInvisible;
	private bool _isPlayerInvisability;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };
    public override States State => States.CreeperInvisible;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
		Debug.Log("EnterState CreeperInvisible");

        _characterState = character;
		_player = _characterState.Character;

		_originalMoveSpeed = _player.Move.CurrentSpeed;
		_originalStaminaRegen = _player.Stamina.RegenerationValue;

        if (_player != null)
        {
            _skills = _player.CharacterState.Character.Abilities.Abilities;
            foreach (Skill ability in _skills)
            {
                if (ability is CreeperInvisible creeperInvisible)
                {
                    if (_creeperInvisible == null)
                    {
                        _creeperInvisible = creeperInvisible;
                    }
                }
            }
        }
    }

	public override void UpdateState()
    {
        _isInvisible = _creeperInvisible.IsInvisible;
		//Debug.Log($"CreeperInvisible / _isInvisible = {_isInvisible}");
        if (_isInvisible)
		{
			if (!_isPlayerInvisability)
			{
				ApplyInvisible();
			}
		}
		else
		{
            ExitState();
		}
	}

    public override void ExitState()
    {
		if (_isPlayerInvisability || !_isPlayerInvisability)
		{
			_isPlayerInvisability = false;
			ResetValues();
			_characterState.RemoveState(this);
		}
    }

    public override bool Stack(float time)
    {
        return false;
    }

	private void ApplyInvisible()
	{
        _isPlayerInvisability = true;

		float reductionMoveSpeed = _originalMoveSpeed * _reductionMoveSpeed;
		float endReductionMoveSpeed = _originalMoveSpeed - reductionMoveSpeed;
		_player.Move.SetMoveSpeed(endReductionMoveSpeed);
		Debug.Log("Player MoveSpeed == " + _player.Move.CurrentSpeed);

        _player.Stamina.RegenerationValue *= (1 + _increaseStaminaRegen);
		Debug.Log("Player StaminaRegen == " + _player.Stamina.RegenerationValue);

		if (!_isIncreasedManaCost)
		{
			foreach (Skill ability in _skills)
			{ 
				ability.Buff.ManaCost.IncreasePercentage(1.3f);
				Debug.Log("Ability manaCost == " + ability.Buff.ManaCost.Multiplier);
				Debug.Log("Modified manaCost at ability: " + ability.name + ", Type: " + ability.GetType() + ", ManaCost Value = " + ability.Buff.ManaCost);
				Debug.Log("IsIncreasedManaCost in Search Abilities== " + _isIncreasedManaCost);
			}
			_isIncreasedManaCost = true;
		}
    }

	private void ResetValues()
	{
		_player.Move.SetDefaultSpeed();
        Debug.Log("Player MoveSpeed == " + _player.Move.CurrentSpeed);

		if (_player.Stamina.RegenerationValue != _originalStaminaRegen)
		{
			_player.Stamina.RegenerationValue /= (1 + _increaseStaminaRegen);
			Debug.Log("Player StaminaRegen == " + _player.Stamina.RegenerationValue);
		}

		if (_isIncreasedManaCost)
		{
			foreach (Skill ability in _skills)
			{
				ability.Buff.ManaCost.ReductionPercentage(1.3f);
				Debug.Log("Ability manaCost == " + ability.Buff.ManaCost.Multiplier);
				Debug.Log("Modified manaCost at ability: " + ability.name + ", Type: " + ability.GetType() + ", ManaCost Value = " + ability.Buff.ManaCost);
			}
			_isIncreasedManaCost = false;
			Debug.Log("IsIncreasedManaCost in ResetValues == " + _isIncreasedManaCost);
		}

		_isPlayerInvisability =	false;
	}
}

#region CreeperDebuffPoisons

public class EmpathicPoisonsState : AbstractCharacterState
{
    private float _baseEvasionValue = 0.1f;
    private float _increasedEvasionValue;
    private float _endEvasionValue;
    private float _originalEvasionValue;

    private float _currentStacks = 0;
    private float _maxStacks = 3;

    private float _timeBetweenToApplyStacks;
    private float _startTimeBetweentoApplyStacks = 1.0f;
    private float _duration;
    private float _baseDuration;
    private float _damageToExit;

    private Vector3 _playerPosition;
    private Vector3 _characterPosition;

    private bool _isInPoisonCloud;
    public bool turnOff = false;

    public float RadiusCloud;

    public GameObject Player;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };
    public override States State => States.EmpathicPoisons;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{ 
        _characterState = character;
        _originalEvasionValue = _characterState.Character.Health.EvadeMeleeDamage;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }
    }

    public override void UpdateState()
    {
        _playerPosition = Player.transform.position;
        _characterPosition = _characterState.transform.position;

        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }

        _timeBetweenToApplyStacks -= Time.deltaTime;
        if (_timeBetweenToApplyStacks <= 0)
        {
            if (_isInPoisonCloud)
            {
                ReducingChanceOfHittingAtEnemy();
            }
            else
            {
                DecreaseEvasionForCurrentTarget();
            }
            _timeBetweenToApplyStacks = _startTimeBetweentoApplyStacks;
        }
        CheckIfInPoisonCloud(_playerPosition, _characterPosition);
    }

    public override void ExitState()
    {
        ResetValues();
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < _maxStacks)
        {
            AddStacks();
            return true;
        }
        else
        {
            _duration = _baseDuration;
            return false;
        }
    }

    private void AddStacks()
    {
        _currentStacks++;
        _duration = _baseDuration;
    }

    private void ReducingChanceOfHittingAtEnemy()
    {
        // Позже будет реализована другая логика.
        _increasedEvasionValue = _baseEvasionValue * _currentStacks;

        _endEvasionValue = _originalEvasionValue * _increasedEvasionValue;

        _characterState.Character.Health.EvadeMeleeDamage = _originalEvasionValue - _endEvasionValue;
    }

    private void DecreaseEvasionForCurrentTarget()
    {
        float reductionPerSecond = _baseEvasionValue * 0.33f;
        _endEvasionValue = Mathf.Max(_originalEvasionValue, _characterState.Character.Health.EvadeMeleeDamage + reductionPerSecond);
        _characterState.Character.Health.EvadeMeleeDamage = _endEvasionValue;
    }

    private void CheckIfInPoisonCloud(Vector3 playerPos, Vector3 characterPos)
    {
        float distance = Vector3.Distance(playerPos, characterPos);
        _isInPoisonCloud = distance <= RadiusCloud;
    }

    private void ResetValues()
    {
        _currentStacks = 0;
        _baseDuration = 0;
        _duration = 0;

        _baseEvasionValue = 0.1f;
        _increasedEvasionValue = 0;
        _endEvasionValue = 0;
        _characterState.Character.Health.EvadeMeleeDamage = _originalEvasionValue;
    }
}

public class PoisonBoneState : AbstractCharacterState
{
	public bool turnOff = false;

	private List<Skill> _skills = new();
	private CreeperStrike _creeperStrike;

	private int _currentStacks = 0;
	private int _maxStacks = 4;

	private float _timeBetweenAttack;
	private float _startTimeBetweenAttack = 1f;

	private float _duration;
	private float _baseDuration;

	private float _baseDamage = 1f;
	private float _endDamage;

	private Character _player;

	public int CurrentStacks { get => _currentStacks; }

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public override States State => States.PoisonBone;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
		_player = personWhoMadeBuff;

        _duration = durationToExit;
        _baseDuration = durationToExit;

		if (_player != null)
		{
			_skills = _player.GetComponent<CharacterState>().Character.Abilities.Abilities;
			//Debug.Log("PoisonBone player == " + _player);

			foreach (Skill ability in _skills)
			{
				//Debug.Log("Checking ability: " + ability.name + ", Type: " + ability.GetType());
				if (ability is CreeperStrike creeperStrike)
				{
					//Debug.Log("if / ability");
					if (_creeperStrike == null)
					{
						_creeperStrike = creeperStrike;
					//Debug.Log("CreeperStrike == " + _creeperStrike);
						_creeperStrike.PoisonBoneStacks(_currentStacks);
					}
				}
			}
		}

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }

    }

    public override void UpdateState()
    {
        _timeBetweenAttack -= Time.deltaTime;
        if (_timeBetweenAttack <= 0)
        {
            DamageDeal();
            _timeBetweenAttack = _startTimeBetweenAttack;
        }

        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }

    }

    public override void ExitState()
    {
        ResetValues();

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
		Debug.Log("PoisonBone Stacks = " + _currentStacks);
        if (_currentStacks < _maxStacks)
        {
            AddStacks();
            return true;
        }
        else
        {
            _duration = _baseDuration;
            return false;
        }
    }

    public void AddStacks()
    {
		if (_currentStacks < _maxStacks)
		{
			_currentStacks++;
			//Debug.Log("if / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
			_duration = _baseDuration;
		}
		else
		{
            //Debug.Log("else / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
    }

	[Server]
    private void DamageDeal()
    {
		Debug.Log("PoisonBone / DamageDeal");
        _endDamage = _currentStacks * _baseDamage;

		Damage damage = new Damage
		{
			Value = _endDamage,
			Type = DamageType.Magical,
			Range = AttackRangeType.Inner
		};

		_characterState.Character.Health.TryTakeDamage(ref damage, _creeperStrike);
    }

    private void ResetValues()
    {
        _currentStacks = 0;
        _baseDuration = 0;
        _duration = 0;
        _endDamage = 0;
        _baseDamage = 1f;
		_timeBetweenAttack = _startTimeBetweenAttack;
    }
}

public class WitheringPoisonState : AbstractCharacterState
{
    public bool turnOff = false;

    private List<Skill> _skills = new();
	private List<Talent> _talents = new();
	private BindingPoison _bindingPoison;
    private Character _player;

    private int _currentStacks = 0;
    private int _maxStacks = 3;

    private float _timeBetweenTakeAwayMana;
    private float _startTimeBetweenTakeAwayMana = 1f;

    private float _duration;
    private float _baseDuration;

    private float _baseValueTakeAwayMana = 0.03f;
    private float _endValueTakeAwayMana;
	private float _chanceOfApplyBindingPoison = 0.9f;

	private bool _isActiveTalentBindingPoison = false;

    public int CurrentStacks { get => _currentStacks; }

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public override States State => States.WitheringPoison;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
		//Debug.Log("Withering Enter State");

        _characterState = character;


        _duration = durationToExit;
        _baseDuration = durationToExit;

		_player = personWhoMadeBuff;

        //Debug.Log("player in WitheringPoisonState == " + _player);
        if (_player != null)
        {
			_talents = _player.CharacterState.Character.GetComponent<HeroComponent>().Talents.Talents;
            //Debug.Log("WitheringPoisonState Talent == " + _talents);

            foreach (Talent talent in _talents)
            {
                //Debug.Log("Checking talents: " + talent.name + ", Type: " + talent.GetType());
                if (talent is BindingPoison bindingPoison)
                {
                    if (_bindingPoison == null)
                    {
                        _bindingPoison = bindingPoison;
						_isActiveTalentBindingPoison = _bindingPoison.IsActive;
                    }
                }
            }
        }

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }
    }

    public override void UpdateState()
    {
        _timeBetweenTakeAwayMana -= Time.deltaTime;
        if (_timeBetweenTakeAwayMana <= 0)
        {
            TakeAwayMana();
            _timeBetweenTakeAwayMana = _startTimeBetweenTakeAwayMana;
        }

        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        ResetValues();

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < _maxStacks)
        {
            AddStacks();
            return true;
        }
        else
        {
            _duration = _baseDuration;
            return false;
        }
    }

    public void AddStacks()
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            //Debug.Log("if / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
        else
        {
            //Debug.Log("else / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
    }

    private void TakeAwayMana()
    {
		Debug.Log("WitheringPoison / TakeAwayMana");
        float takeAwayMana = _currentStacks * _baseValueTakeAwayMana;
        _endValueTakeAwayMana = _characterState.Character.Stamina.CurrentValue * takeAwayMana;

		if (_isActiveTalentBindingPoison)
		{
			if (Random.Range(0.0f, 1.0f) <= _chanceOfApplyBindingPoison)
			{
				_characterState.AddState(States.BindingPoison, 10, 0, _player.gameObject, null);
            }
        }

		_characterState.Character.Stamina.ReductionCurrentValue(_endValueTakeAwayMana);
    }

    private void ResetValues()
    {
        _currentStacks = 0;
        _baseDuration = 0;
        _duration = 0;
        _endValueTakeAwayMana = 0;
        _baseValueTakeAwayMana = 1f;
        _timeBetweenTakeAwayMana = _startTimeBetweenTakeAwayMana;
    }
}


public class BindingPoisonState : AbstractCharacterState
{
    public bool turnOff = false;

    private static int _currentStacks = 0;
    private int _maxStacks = 1;

    private float _duration;
    private float _baseDuration;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public override States State => States.BindingPoison;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;

		//Debug.Log($"BindingPoisonState / EnterState / CharacterManager = {_skillManager}");

        _duration = durationToExit;
        _baseDuration = durationToExit;

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }
        BlockingOrCancleingAbility();
    }

    public override void UpdateState()
    {
        //Debug.Log($"BindingPoisonState / UpdateState / CharacterManager = {_skillManager}");
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }

    }

    public override void ExitState()
    {
        //Debug.Log($"BindingPoisonState / ExitState / CharacterManager = {_skillManager}");
        ResetValues();

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        //Debug.Log($"BindingPoisonState / Stack / CharacterManager = {_skillManager}");
        if (_currentStacks < _maxStacks)
        {
            AddStacks();
            return true;
        }
        else
        {
            _duration = _baseDuration;
            return false;
        }
    }

    public void AddStacks()
    {
        if (_currentStacks < _maxStacks)
        {
			_currentStacks++;
            //Debug.Log("if / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
        else
        {
            //Debug.Log("else / CurrentStackPoisonBone in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
    }

    private void BlockingOrCancleingAbility()
    {
        Debug.Log("BindingPoison / BlockingOrCancleingAbility");
       // Debug.Log($"BindingPoisonState / BlockingOrCancleingAbility / CharacterManager = {_skillManager}");

		_abilities.SkillQueue.TryCancel(true);
		//Debug.Log($"BindingPoison / BlockingOrCancleingAbility / skillManager.TryCancel = {_skillManager.SkillQueue.TryCancel(true)}");

		if (!_abilities.SkillQueue.TryCancel(true))
		{
            Debug.Log("BindingPoison / BlockingOrCancleingAbility / TryCancel = false");
            _abilities.SkillQueue.SkillAdded += OnSkillAdded;
			Debug.Log($"BindingPoison / BlockingOrCancleingAbility / after SkillAdded += OnSkillAdded");
        }
		ExitState();
    }

	private void OnSkillAdded(Skill skill)
	{
		Debug.Log("BindingPoison / OnSkillAdded Start");
        Debug.Log($"BindingPoison / OnSkillAdded / CurrentSkill = {_abilities.SkillQueue.CurrentSkill}");

		Debug.Log($"BindingPoison / OnSkillAdded / _skillManager.SkillQueue.TryCancel(true) = {_abilities.SkillQueue.TryCancel(true)}");
        _abilities.SkillQueue.TryCancel(true);

        _abilities.SkillQueue.SkillAdded -= OnSkillAdded;
		Debug.Log("BindingPoison / OnSkillAdded End");
    }

    private void ResetValues()
    {
        _currentStacks = 0;
        _baseDuration = 0;
        _duration = 0;
    }
}

#endregion

#region PoisonClouds

public class PoisonCloudState : AbstractCharacterState
{
    public bool turnOff = false;

    private List<Skill> _skills = new();
	private List<Talent> _talents = new();

	private EmpathicPoisonsState _empathicPoisons;
	private CapaciousPoisonCloud _capaciousPoisonCloud;
    private ToxiqueCloud _toxiqueCloud;
	private ExplosionPoisonCloud _cloudExplosion;

    private Character _player;
	private LayerMask _enemiesLayer;

    private int _currentStacks = 0;
    private int _maxStacks = 5;
    private float _radiusCloud = 5.5f;

    private float _baseDamage = 0.005f;
    private float _increasedDamage;
    private float _endDamage;

    private float _timeBetweenAttack;
    private float _startTimeBetweenAttack = 1f;

    private float _duration;
    private float _baseDuration;
    private float _durationEmpathicPoisons = 3f;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public override States State => States.PoisonCloud;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
		//Debug.Log("EnterState PoisonCloud");
        _characterState = character;
        _toxiqueCloud = _characterState.GetComponentInChildren<ToxiqueCloud>();
		_player = personWhoMadeBuff;

        _duration = durationToExit;
        _baseDuration = durationToExit;

		_timeBetweenAttack = _startTimeBetweenAttack;

		//_empathicPoisons = _player.CharacterState.GetComponent<EmpathicPoisons>();
		//Debug.Log($"_empathicPoisons in PoisonCloud = {_empathicPoisons}");


		if (_player != null)
		{
		    _skills = _player.CharacterState.Character.Abilities.Abilities;
			_talents = _player.CharacterState.Character.GetComponent<HeroComponent>().Talents.Talents;

            SearchAbilities();

			SearchTalent();
		}

		if (_capaciousPoisonCloud != null && _capaciousPoisonCloud.IsActive)
		{
			_radiusCloud += 1.5f; 
		}

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }
    }

	private void SearchAbilities()
	{
        foreach (Skill ability in _skills)
        {
            if (ability is ExplosionPoisonCloud cloudExplosion)
            {
                if (_cloudExplosion == null)
                {
                    _cloudExplosion = cloudExplosion;
					//Debug.Log($"PoisonCloud / SearchAbilities / cloudExplosion = {_cloudExplosion}");
                }
            }
        }
    }

	private void SearchTalent()
	{
        foreach (Talent talent in _talents)
        {
			if (talent is CapaciousPoisonCloud capaciousCloud)
			{
				if (_capaciousPoisonCloud == null)
				{
					_capaciousPoisonCloud = capaciousCloud;
                }
			}
		}
	}

    public override void UpdateState()
    {
        _timeBetweenAttack -= Time.deltaTime;
        if (_timeBetweenAttack <= 0)
        {
			//Debug.Log("PoisonCloud / timeBetweenAttack <= 0");
            SearchingEnemies();
            _timeBetweenAttack = _startTimeBetweenAttack;
        }

        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }

    }

    public override void ExitState()
    {
		//Debug.Log("PoisonCloud / ExitState");
        ResetValues();

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        //Debug.Log($"PoisonCloud / Stack / currentStacks = {_currentStacks}");

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
			if (_cloudExplosion != null)
			{
				_cloudExplosion.CurrentStacksPoisonCloud(_currentStacks, _radiusCloud);
				//Debug.Log($"PoisonCloud / Stack / if / CurrentStacks/RadiusCloud = {_currentStacks}, {_radiusCloud}");
			}
            return true;
        }
        else
        {
            _duration = _baseDuration;
            if (_cloudExplosion != null)
            {
                _cloudExplosion.CurrentStacksPoisonCloud(_currentStacks, _radiusCloud);
               // Debug.Log($"PoisonCloud / Stack / else / CurrentStacks/RadiusCloud = {_currentStacks}, {_radiusCloud}");
            }
            return false;
        }
    }

    public void AddStacks()
    {
		if (_currentStacks < _maxStacks)
		{
            _currentStacks++;
			_duration = _baseDuration;
			//Debug.Log("PoisonCloud AddStacks = " + _currentStacks);
          //  Debug.Log("if / CurrentStackPoisonCloud in AddStacks == " + _currentStacks); 
		}
		else
		{
			//Debug.Log("else / CurrentStackPoisonCloud in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
		}
    }

    private void SearchingEnemies()
    {
		Debug.Log($"PoisonCloud / SearchingEnemies");
        
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(_characterState.transform.position, _radiusCloud);
        Debug.Log($"PoisonCloud / SearchingEnemies / after hitEnemies = {hitEnemies}");
		foreach (Collider2D enemy in hitEnemies)
		{
			if (enemy.CompareTag("Enemies"))
			{
				Debug.Log($"PoisonCloud / SearchingEnemies / enemy = {enemy}");
				if (enemy.TryGetComponent<Character>(out var target) && enemy.transform != _characterState.transform)
				{
					DamageDeal(target);
					Debug.Log("After TryGetComponent");
					_timeBetweenAttack = _startTimeBetweenAttack;
				}
			}
		}
    }

    private void DamageDeal(Character target)
    {
        Debug.Log($"PoisonCloud / DamageDeal");
        _increasedDamage = _baseDamage * _currentStacks;
        Debug.Log($"PoisonCloud / DamageDeal / _increasedDamage = {_increasedDamage}");
        _endDamage = target.Health.MaxValue * _increasedDamage;
        Debug.Log($"PoisonCloud / DamageDeal / _endDamage = {_endDamage}");

        Damage damage = new Damage()
		{
			Value = _endDamage,
			Type = DamageType.Physical,
			Range = AttackRangeType.MeleeAttack
		};
        Debug.Log($"PoisonCloud / DamageDeal / damage = {damage}");

        target.Health.TryTakeDamage(ref damage, _cloudExplosion);

        if (_toxiqueCloud.IsActive)
        {
			target.CharacterState.CmdAddState(States.EmpathicPoisons, _durationEmpathicPoisons, 0, _player.gameObject, null);
            _empathicPoisons.Player = _player.gameObject;
            _empathicPoisons.RadiusCloud = _radiusCloud;
        }
    }

    private void ResetValues()
    {
        _currentStacks = 0;
        _baseDuration = 0;
        _duration = 0;
        _endDamage = 0;
        _increasedDamage = 0;
        _baseDamage = 0.005f;
    }
}

public class HealingPoisonCloudState : AbstractCharacterState
{
    public bool turnOff = false;

    //private List<Skill> _skills = new();
    //private List<Talent> _talents = new();

    private int _currentStacks = 0;
    private int _maxStacks = 5;
    private float _radiusCloud = 3.5f;

    private float _baseHeal = 0.005f;
    private float _increasedHeal;
    private float _endHeal;

    private float _timeBetweenHeal;
    private float _startTimeBetweenHeal = 1f;

    private static float _duration;
    private static float _baseDuration;

    private Character _player;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public override States State => States.HealingPoisonCloud;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _player = _characterState.Character;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }

        //if (_player != null)
        //{
        //    _skills = _player.CharacterState.Character.Abilities.Abilities;
        //    Debug.Log("PoisonCloud player == " + _player);
        //}
    }

    public override void UpdateState()
    {

        _timeBetweenHeal -= Time.deltaTime;
        if (_timeBetweenHeal <= 0)
        {
            SearchingEnemies();
            _timeBetweenHeal = _startTimeBetweenHeal;
        }

        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }

    }

    public override void ExitState()
    {
        ResetValues();

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < _maxStacks)
        {
            AddStacks();
            return true;
        }
        else
        {
            _duration = _baseDuration;
            return false;
        }
    }

    public void AddStacks()
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            _duration = _baseDuration;
            //Debug.Log("if / CurrentStackPoisonCloud in AddStacks == " + _currentStacks); 
        }
        else
        {
            //Debug.Log("else / CurrentStackPoisonCloud in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
    }

    private void SearchingEnemies()
    {
        Collider2D[] hitAllies = Physics2D.OverlapCircleAll(_characterState.transform.position, _radiusCloud);
        foreach (Collider2D alliesOrPlayer in hitAllies)
        {
            if (alliesOrPlayer.CompareTag("Allies"))
            {
                if (alliesOrPlayer.TryGetComponent<HeroComponent>(out var target))
                {
                    ApplyHealing(target);
                    _timeBetweenHeal = _startTimeBetweenHeal;
                }
            }
        }
    }

    private void ApplyHealing(HeroComponent targetHealth)
    {
		_increasedHeal = _baseHeal * _currentStacks;
		_endHeal = targetHealth.Health.MaxValue * _increasedHeal;
		targetHealth.Health.Heal(_endHeal);
    }

    private void ResetValues()
    {
        _currentStacks = 0;
        _baseDuration = 0;
        _duration = 0;
        _endHeal = 0;
        _increasedHeal = 0;
        _baseHeal = 0.005f;
    }
}

#endregion

#region HealingPoisons

public class HealingPoisonPerSecondState : AbstractCharacterState
{
	//private List<Talent> _talents = new();
	//private SurgeTreatment _surgeTreatment;
	public bool turnOff = false;

	private int _currentStack = 0;
	private int _maxStack = 6;

	private float _baseHealingValue;
	private float _totalHealed = 0.0f;
	private float _currentHealingValue;

    private float _timeBetweenHeal;
	private float _startTimeBetweenHeal = 1.0f;

	private float _duration;
	private float _baseDuration;

	private Character _player;
    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public override States State => States.HealingPoisonPerSecond;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
		Debug.Log("HealingPoisonPerSecond / EnterState");
        Debug.Log("HealingPoisonPerSecond / EveryState = NewState");
        
        _characterState = character;
		_player = personWhoMadeBuff;

        _currentHealingValue = 0.0f;

        _duration = durationToExit; 
        _baseDuration = durationToExit;
    }

    public override void UpdateState()
    {
        _timeBetweenHeal -= Time.deltaTime;
        if (_timeBetweenHeal <= 0)
        {
			if (_currentStack < _maxStack)
			{
				Debug.Log($"HealingPoisonPerSecond / UpdateState / _baseHealingValue = {_baseHealingValue}");
				MakeHeal();
			}
			else
			{
				return;
			}
            _timeBetweenHeal = _startTimeBetweenHeal;
        }

        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
		Debug.Log("HealingPoisonPerSecond / Stack");
        return false;
    }

    private void MakeHeal()
    {
        _currentHealingValue += 1.0f;

		Debug.Log($"HealingPoisonPerSecond / MakeHeal / _currentHealingValue = {_currentHealingValue}");

        _characterState.Character.Health.Heal(_currentHealingValue);
    }
}

public class InstantHealingPoisonState : AbstractCharacterState
{
    //private List<Talent> _talents = new();
    //private SurgeTreatment _surgeTreatment;
    public bool turnOff = false;

    private Character _player;

    private int _currentStacks = 0;
    private int _maxStacks = 1;

    private float _baseHealingValue = 14.0f;
	private float _healingValuePerSecond;

    private float _totalHealed = 0.0f;

    private float _timeBetweenHeal;
    private float _startTimeBetweenHeal = 1.0f;

    private float _duration;
    private float _baseDuration;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public override States State => States.InstantHealingPoison;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("InstantHealingPoison / EnterState");

        _characterState = character;

        _duration = durationToExit;
        _baseDuration = durationToExit;
		_player = personWhoMadeBuff;

        //Debug.Log("_player == " + _player);

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }
        //Debug.Log($"SetPlayer in EnterHealingPoisonState == {_player}");

        //if (_player != null)
        //{
        //	_talents = _player.CharacterState.Character.TalentSystem.Talents;
        //	Debug.Log("HealingPoison player == " + _player);

        //	foreach (Talent talent in _talents)
        //	{
        //		Debug.Log("Checking talents: " + talent.name + ", Type: " + talent.GetType());
        //		if (talent is SurgeTreatment surgeTreatment)
        //		{
        //			Debug.Log("if / talents");
        //			if (_surgeTreatment == null)
        //			{
        //				_surgeTreatment = surgeTreatment;
        //				Debug.Log("SurgeTreatment == " + _surgeTreatment);
        //			}
        //		}
        //	}
        //}
    }

    public override void UpdateState()
    {
        _timeBetweenHeal -= Time.deltaTime;
        if (_timeBetweenHeal <= 0)
        {
            MakeHeal();
            _timeBetweenHeal = _startTimeBetweenHeal;
        }

        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }

    public void AddStacks()
    {
        Debug.Log("InstantHealingPoison / AddStacks");
        _currentStacks++;
        _duration = _baseDuration;
    }

    private void MakeHeal()
    {
        _characterState.Character.Health.Heal(_baseHealingValue);
        //if (_surgeTreatment != null && _surgeTreatment.IsActive)
        //{
        //	_totalHealed += _baseHealingValue;
        //	Debug.Log("TotalHeal == " + _totalHealed);
        //}
    }

    //private void InstantHeal()
    //{
    //      Debug.Log("Instant Heal Method");
    //     _characterState.Health.AddHeal(_totalHealed);
    //      _totalHealed = 0.0f;
    //}
}



public class RegeneratingPoisonState : AbstractCharacterState
{
	public bool turnOff = false;

	private List<Talent> _talents = new();
	private static SurgeTreatment _surgeTreatment;

	private Character _player;
	private CharacterState _character;

	private int _currentStacks = 0;
	private int _maxStacks = 5;

    private float _baseHealingValue = 1.0f;
	private float _endHealingValue;
	private float _totalHeal;

    private float _timeBetweenHeal;
    private float _startTimeBetweenHeal = 1.0f;

    private float _duration;
    private float _baseDuration;

	    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public override States State => States.RegeneratingPoison;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
		_character = character;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }
		Debug.Log("_player in EnterRegenPoisonState == " + _player);
        if (_player != null)
        {
			_talents = _player.CharacterState.Character.GetComponent<HeroComponent>().Talents.Talents;
			Debug.Log("HealingPoison player == " + _player);

            foreach (Talent talent in _talents)
            {
                Debug.Log("Checking talents: " + talent.name + ", Type: " + talent.GetType());
                if (talent is SurgeTreatment surgeTreatment)
                {
					Debug.Log("if / talents");
                    if (_surgeTreatment == null)
                    {
                        _surgeTreatment = surgeTreatment;
        				Debug.Log("SurgeTreatment == " + _surgeTreatment);
                    }
                }
            }
        }
    }

    public override void UpdateState()
    {
        _timeBetweenHeal -= Time.deltaTime;
        if (_timeBetweenHeal <= 0)
        {
            MakeHeal();
            _timeBetweenHeal = _startTimeBetweenHeal;
        }

        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        ResetValues();

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < _maxStacks)
        {
            AddStacks();
            return true;
        }
        else
        {
            _duration = _baseDuration;
            return false;
        }
    }

    public void AddStacks()
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            Debug.Log("if / CurrentStackHealingPoison in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
        else
        {
            Debug.Log("else / CurrentStackHealingPoison in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
    }

    private void MakeHeal()
    {
		Debug.Log("RegenerationPoison / MakeHeal");
		_endHealingValue = _currentStacks * _baseHealingValue;
		_characterState.Character.Health.Heal(_endHealingValue);
		if (_surgeTreatment != null && _surgeTreatment.IsActive)
		{
			_totalHeal += _endHealingValue;
			Debug.Log("TotalHeal RegenerationPoison == " + _totalHeal);
		}
    }

    private void ResetValues()
    {
        _currentStacks = 0;
        _baseDuration = 0;
        _duration = 0;
    }

	public void InstantHeal()
	{
		if (_surgeTreatment != null)
		{
			float totalHeal = _totalHeal;
			Debug.Log("InstantHeal // totalHeal == " + totalHeal);	
			_character.Character.Health.Heal(totalHeal);
			_totalHeal = 0;
		}
	}
}

#endregion

public class HeatedGlandsState : AbstractCharacterState
{
	private int _currentStacks;
	private int _maxStacks = 10;

	private float _duration;
	private float _baseDuration;
	private float _amountManaIncreasingValue = 0.02f;
	private float _newMaxManaPlayer;
	private float _maxManaPlayer;

	private Character _player; 
	private Resource _playerMana;

    private List<StatusEffect> _effects = new List<StatusEffect>();

    public override States State => States.HeatedGlands;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
		_characterState = character;
		_player = personWhoMadeBuff;
		_playerMana = _player.TryGetResource(ResourceType.Mana);

		_duration = durationToExit;
		_baseDuration = _duration;

		_maxManaPlayer = _playerMana.MaxValue;

		if (_currentStacks < _maxStacks)
		{
            AddStack();
		}

    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
		if (_duration < 0)
		{
			ExitState();
		}
    }

    public override void ExitState()
    {
        _playerMana.ReductionMaxValue(_newMaxManaPlayer);

		_currentStacks = 0;
        _newMaxManaPlayer = 0;

		_characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < _maxStacks)
        {
            AddStack();
			return true;
		}
		else
		{
			_duration = _baseDuration;
			return false;
		}
    }

	private void AddStack()
    {
        _currentStacks++;
		_duration = _baseDuration;
		IncreasingAmountManaValue();
    }

    private void IncreasingAmountManaValue()
    {
        Debug.Log("HeatedGlands / IncreasingAmountManaValue");

        float bonusAmountMana = _amountManaIncreasingValue * _maxManaPlayer;
        Debug.Log("IncreasingMana / bonusMana == " + bonusAmountMana);

        _playerMana.IncreaseMaxValue(bonusAmountMana);

		_newMaxManaPlayer += bonusAmountMana;
        Debug.Log("IncreasingMana / newMaxManaPlayer == " + _newMaxManaPlayer);

       Debug.Log("IncreasingMana / MaxManaPlayer after +bonusMana == " + _playerMana.MaxValue);
	}
}

#endregion

public class AbilitySchoolDebuff : AbstractCharacterState
{
	public bool turnOff = false;
	private float _baseDuration;
	private float _duration;
	public Schools canceledSchoool;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.AbilitySchool};

	public override States State => States.SchoolDebuff;
	public override StateType Type => StateType.Immaterial;
	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		Debug.Log("Entering AbilitySchoolDebuff State");

		_characterState = character;

		Debug.Log("CHECK FOR SCHOOL " + canceledSchoool);
		if (character.TryGetComponent<Character>(out var ability))
		{
			_abilities = ability.Abilities;
			_abilities.SwitchAvaliable(canceledSchoool, false);
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}
		_duration = durationToExit;
		_baseDuration = durationToExit;
	}

	public override void UpdateState()
	{
		Debug.Log("Updating AbilitySchoolDebuff State");
		_duration -= Time.deltaTime;
		if (_duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting AbilitySchoolDebuff State");
		if (_characterState.Check(StatusEffect.Ability) && _abilities != null)
		{
			_abilities.SwitchAvaliable(canceledSchoool, true);
		}
		_characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		Debug.Log("STACKING TEST");
		if (_duration > time)
		{
			Debug.Log("STACKING TEST 2 2 2");
			return true;
		}
		else
		{
			Debug.Log("STACKING TEST 3 3 3");
			_duration = time;
			return true;
		}
	}
}

public class AbilityFormDebuff : AbstractCharacterState
{
	public bool turnOff = false;
	private float _baseDuration;
	private float _duration;
	public AbilityForm canceledForm;
	public bool canCancel = false;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.AbilitySchool};

	public override States State => States.FormDebuf;
	public override StateType Type => StateType.Immaterial;
	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		Debug.Log("Entering AbilityFormDebuff State");
		_characterState = character;

		Debug.Log("CHECK FOR FORM " + canceledForm);

		if (character.TryGetComponent<Character>(out var ability))
		{
			_abilities = ability.Abilities;
			_abilities.SwitchAvaliable(canceledForm, false);
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}
		_duration = durationToExit;
		_baseDuration = durationToExit;
	}

	public override void UpdateState()
	{
		Debug.Log("Updating AbilityFormDebuff State");
		_duration -= Time.deltaTime;
		if (_duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting AbilityFormDebuff State");
		if (_characterState.Check(StatusEffect.Ability) && _abilities != null)
		{
			_abilities.SwitchAvaliable(canceledForm, true);
		}
		_characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		
		if (_duration > time)
		{
			return true;
		}
		else
		{
			_duration = time;
			return true;
		}
	}
}

public class Plague : AbstractCharacterState
{
	private int _stack = 0;
	private float _durationToExit = 0f;
	private float _damageTimer = 1f;
	public int GetStack => _stack;
	public override States State => States.Plague;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => throw new System.NotImplementedException();

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		Debug.Log("Entering Plague State");
		_characterState = character;
		_durationToExit = durationToExit;
		_health = _characterState.Character.Health;
		_abilities = character.Character.Abilities;

		for(int i = 0; i<_abilities.Abilities.Count; i++) 
		{
			_abilities.Abilities[i].Buff.Damage.ReductionPercentage(0.05f);
		}
		// reduce damage given
	}

	public override void UpdateState()
	{
		_durationToExit -= Time.deltaTime;
		_damageTimer -= Time.deltaTime;

		if (_damageTimer <= 0)
		{
			_damageTimer = 1;
			int damage = Random.Range(1, 4);
			MakeDamage(damage);


			//_health.TryTakeDamage(damage, DamageType.Magical, AttackRangeType.MeleeAttack);
			if (Random.Range(0, 100) < 50 && _personWhoMadeBuff != null)
			{
				/*DeathSpiral deathSpiral = (DeathSpiral)_characterState.personWhoShoted.Abilities.GetAbilityByName("DeathSpiral");
				if(deathSpiral != null) 
				{
					Debug.Log("ADD CHRAGE");
					deathSpiral.AddCharge();
				}*/
			}

			if (Random.Range(0, 5) < 1)
			{
				AddState();
			}
			//20% chance of inflicting close enemy
		}
		if(_durationToExit <= 0) 
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Plague State");
		// return reduced damage given
		for (int i = 0; i < _abilities.Abilities.Count; i++)
		{
			_abilities.Abilities[i].Buff.Damage.IncreasePercentage(0.05f);
		}
		_characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		if(_stack <= 4)
		{
			_durationToExit = time;
			_stack++;
			return true;
		}
        else
        {
			_durationToExit = time;
            return true;
        }
    }

	[Server]
	private void MakeDamage(float damage)
	{
		//_health.TryTakeDamage(damage, DamageType.Magical, AttackRangeType.MeleeAttack);
	}
	[Server]
	private void AddState()
	{
		Collider2D[] colliders = Physics2D.OverlapCircleAll(_characterState.gameObject.transform.position, 3);

		foreach (Collider2D collider in colliders)
		{
			if (collider.TryGetComponent<Character>(out var enemy) && collider.gameObject != _characterState.gameObject)
			{
				//enemy.Health.TryTakeDamage(damage / 2, DamageType.Magical, AttackRangeType.RangeAttack);
				enemy.CharacterState.CmdAddState(States.Plague, 4, 0, null, null);
			}
		}
	}
}

public class NorthernerEndurance : AbstractCharacterState
{
	private float _durationToExit;
	private float _damageToExit;

	public override States State => States.NorthernerEndurance;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => throw new System.NotImplementedException();

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_health = character.Character.Health;
		//_health.BoostHpBonus(damageToExit);
		_durationToExit = durationToExit;
		_damageToExit = damageToExit;
	}

	public override void UpdateState()
	{
		_durationToExit-=Time.deltaTime;
		if(_durationToExit < 0)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		//_health.BoostHpReverse(_damageToExit);
	}

	public override bool Stack(float time)
	{
		_durationToExit = time;
		return true;
	}	
}

public class Curse : AbstractCharacterState
{
	private Character _personWhoShooted;
	private float _durationToExit = 0;

	public override States State => States.Curse;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => throw new System.NotImplementedException();

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_characterState = character;
		_durationToExit = durationToExit;
		//if(character.personWhoShoted != null)
		//_personWhoShooted = character.personWhoShoted;
	}

	public override void UpdateState()
	{
		_durationToExit -= Time.deltaTime;
		if( _durationToExit < 0 )
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		_characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		/*if (_characterState.personWhoShoted != null)
		{
			_personWhoShooted = _characterState.personWhoShoted;
		}*/
		return true;
	}
}

public class LastBreath : AbstractCharacterState
{
	private Character _character;
	private float _durationToExit = 0;

	public override States State => States.LastBreath;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => throw new System.NotImplementedException();

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_character = character.Character;
		_abilities = _character.Abilities;
		_durationToExit = durationToExit;
		_health = _character.Health;

		_character.Move.ChangeMoveSpeed(1.2f);
		for(int i = 0; i < _abilities.Abilities.Count; i++) 
		{
			_abilities.Abilities[i].Buff.AttackSpeed.IncreasePercentage(1.4f);
		}
		_health.RegenerationValue *= 4;
		//increase -regen
	}

	public override void UpdateState()
	{
		_durationToExit -= Time.deltaTime;
		if(_durationToExit < 0)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		//decrease -regen
		//_character.Move.ChangeMoveSpeedBack(1.2f);
		for (int i = 0; i < _abilities.Abilities.Count; i++)
		{
			_abilities.Abilities[i].Buff.AttackSpeed.ReductionPercentage(1.4f);
		}
		_health.RegenerationValue /= 4;
	}

	public override bool Stack(float time)
	{
		return true;
	}		
}

public class MagicBuff : AbstractCharacterState
{
	private Character _character;
	private float _durationToExit;
	private float _shieldCapacity;

	public override States State => States.MagicBuff;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => throw new System.NotImplementedException();

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_character = character.Character;
		_durationToExit = durationToExit;
		_shieldCapacity = damageToExit;

		//_character.Health.SetMagAbsorb(_shieldCapacity);
	}

	public override void UpdateState()
	{
		_durationToExit -= Time.deltaTime;
		if(_durationToExit < 0)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		//_character.Health.SetMagAbsorb(0);
	}

	public override bool Stack(float time)
	{
		_durationToExit = time;
		return true;
	}
}

public class CharacterState : NetworkBehaviour
{
	/*private Health _health;
	private MoveComponent _move;
	private Resource _stamina;*/
	private Character _hero;

	private List<AbstractCharacterState> _currentStates = new List<AbstractCharacterState>();
	[SerializeField] private StateIcons _stateIcons;

	public bool invinsible = false;
	/*public Health Health => _health;
	public MoveComponent Move => _move;
	public Resource Stamina => _stamina;*/
	public Character Character => _hero;

	public bool Invinsible = false;

	public Dictionary<States, AbstractCharacterState> EnumToState = new Dictionary<States, AbstractCharacterState>()
	{
		#region CreeperStates
		[States.CreeperInvisible] = new CreeperInvisibleState(),
		[States.PoisonBone] = new PoisonBoneState(),
		[States.WitheringPoison] = new WitheringPoisonState(),
		[States.BindingPoison] = new BindingPoisonState(),
		[States.PoisonCloud] = new PoisonCloudState(),
		[States.HealingPoisonCloud] = new HealingPoisonCloudState(),
		[States.EmpathicPoisons] = new EmpathicPoisonsState(),
		[States.HealingPoisonPerSecond] = new HealingPoisonPerSecondState(),
		[States.InstantHealingPoison] = new InstantHealingPoisonState(),
		[States.RegeneratingPoison] = new RegeneratingPoisonState(),
		[States.HeatedGlands] = new HeatedGlandsState(),
        #endregion

		[States.Immateriality] = new ImmaterialityState(),
		[States.Stun] = new StunnedState(),
		[States.Frozen] = new FrozenState(),
		[States.Frosting] = new FrostingState(),
		[States.Cooling] = new Cooling(),
		[States.InAir] = new InAirState(),
        [States.Blind] = new BlindnessState(),
		[States.Invisible] = new InvisibleState(),
		[States.SchoolDebuff] = new AbilitySchoolDebuff(),
		[States.Desiccuration] = new Desiccuration(),
		[States.Plague] = new Plague(),
		[States.Curse] = new Curse(),
		[States.NorthernerEndurance] = new NorthernerEndurance(),
		[States.LastBreath] = new LastBreath(),
		[States.MagicBuff] = new MagicBuff(),
	};

	public void Initialize(Character hero)
	{
		_hero = hero;
		/*_health = health;
		_move = move;
		_stamina = stamina;*/
		if (_hero == null)
		{
			Debug.LogError("No required component in " + name + " " + gameObject.name);
		}
	}

	private void Update()
	{
		if (_currentStates.Count > 0)
		{
			for (int i = 0; i < _currentStates.Count; i++)
			{
				_currentStates[i].UpdateState();
			}
		}
	}

	public void Dispel(StateType type)
	{
		foreach (AbstractCharacterState state in _currentStates)
		{
			if (state.Type == type)
			{
				state.ExitState();
			}
		}
	}

	public bool Check(StatusEffect effect)
	{
		foreach (AbstractCharacterState state in _currentStates)
		{
			if (state.Effects.Contains(effect))
			{
				return false;
			}
		}
		return true;
	}

	public bool CheckForState(States state)
	{
		foreach (AbstractCharacterState states in _currentStates)
		{
			Debug.Log(states.State + " on enemy, check for " + state);
			if (states.State == state)
			{
				return true;
			}
		}
		return false;
	}

	public AbstractCharacterState GetState(States state)
	{
		foreach (AbstractCharacterState states in _currentStates)
		{
			Debug.Log(states.State + " on enemy, check for " + state);
			if (states.State == state)
			{
				return states;
			}
		}
		return null;
	}

	[Command]
	public void CmdAddState(States state, float duration, float damageToExit, Schools schools, GameObject personWhoShooted, string skillName)
	{
		AddStateLogic(state, duration, damageToExit, schools, personWhoShooted, skillName);
		ClientAddState(state, duration, damageToExit, schools, personWhoShooted, skillName);
	}

	[Command]
	public void CmdAddState(States state, float duration, float damageToExit, GameObject personWhoShooted, string skillName)
	{
		Debug.Log("Add state cmd");
		AddStateLogic(state, duration, damageToExit, Schools.None, personWhoShooted, skillName);
		ClientAddState(state, duration, damageToExit, Schools.None, personWhoShooted, skillName);
	}

	public void AddState(States state, float duration, float damageToExit, GameObject personWhoShooted, string skillName)
	{
		Debug.Log("Add state from server");
		AddStateLogic(state, duration, damageToExit, Schools.None, personWhoShooted, skillName);
		ClientAddState(state, duration, damageToExit, Schools.None, personWhoShooted, skillName);
	}

	[Command]
	public void CmdRemoveState(States state)
	{
		RemoveStateLogic(state);
		ClientRemoveState(state);
	}

	public void RemoveState(States state)
	{
		RemoveStateLogic(state);
		ClientRemoveState(state);
	}

	public void RemoveState(AbstractCharacterState newState)
	{
		if (_currentStates.Contains(newState))
		{
			//newState.ExitState(this);
			//_stateIcons.RemoveItemByState(newState.state);
			_currentStates.Remove(newState);
		}
	}

	private void RemoveStateLogic(States stateName)
	{
		if (_currentStates.Count <= 0) return;

		_stateIcons.RemoveItemByState(stateName);
		for(int i = _currentStates.Count - 1; i >= 0; i --)
		{
			if (_currentStates[i].State == stateName)
			{
				_currentStates[i].ExitState();
			}
		}
	}

	[ClientRpc]
	private void ClientAddState(States state, float duration, float damageToExit, Schools schools, GameObject personWhoShooted, string skillName)
	{
		Debug.Log("Add state rpc");
		AddStateLogic(state, duration, damageToExit, schools, personWhoShooted, skillName);
	}

	[ClientRpc]
	private void ClientRemoveState(States stateName)
	{
		RemoveStateLogic(stateName);
	}

    private void AddStateLogic(States state, float duration, float damageToExit, Schools school, GameObject personWhoShooted, string skillName)
    {
        Debug.Log("Add state logic");
        if (invinsible)
            return;
        if (CheckForState(state))
        {
            for (int i = 0; i < _currentStates.Count; i++)
            {
                if (_currentStates[i].State != state) continue;

                if (_currentStates[i].Stack(duration))
                {
                    _stateIcons.ActivateIco(state, duration, 1, true);
                }
                else
                {
                    CreateState(EnumToState[state], state, duration, damageToExit, personWhoShooted, skillName, false);
                    break;
                    //nothing at this time??
                }
            }
        }
        else
        {
            CreateState(EnumToState[state], state, duration, damageToExit, personWhoShooted, skillName, false);

            if (school != Schools.None)
            {
                var counterSpell = (AbilitySchoolDebuff)EnumToState[state];
                counterSpell.canceledSchoool = school;
            }
        }
    }

    private void CreateState(AbstractCharacterState state, States stateName, float duration, float damageToExit, GameObject personWhoShooted, string skillName, bool stack)
    {
        _stateIcons.ActivateIco(stateName, duration, 1, stack);
        _currentStates.Add(state);
        if (personWhoShooted.TryGetComponent<Character>(out var character))
        {
            _currentStates[_currentStates.Count - 1].EnterState(this, duration, damageToExit, character, skillName);
        }
        else
        {
            _currentStates[_currentStates.Count - 1].EnterState(this, duration, damageToExit, null, skillName);
        }
    }
}

public enum StateType
{
	Physical,
	Magic,
	Immaterial
}

public enum StatusEffect
{
	Move,
	MoveSpeed,
	Ability,
	AbilitySchool,
	AbilitySpeed,
	Others
}
public enum States
{
    #region CreeperStates
    CreeperInvisible,
	PoisonBone,
	WitheringPoison,
	BindingPoison,
	PoisonCloud,
	HealingPoisonCloud,
	EmpathicPoisons,
	HealingPoisonPerSecond,
	InstantHealingPoison,
	RegeneratingPoison,
	HeatedGlands,
    #endregion

	Default,
    Immateriality,
    Stun,
	Frozen,
	Frosting,
	Cooling,
	InAir,
    Blind,
	Invisible,
	SchoolDebuff,
	FormDebuf,
	Desiccuration,
	Plague,
	Curse,
	NorthernerEndurance,
	LastBreath,
	MagicBuff
}

