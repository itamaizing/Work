using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public abstract class AbstractCharacterState
{
	public StateType type;
	public States state;
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

public class InvisibleStateOld : AbstractCharacterState
{
	public new States state = States.Invisible;

	private Renderer[] childRenderers;
	private GameObject _player;

	private List<GameObject> _enemies = new List<GameObject>();

	private float lastCheckTime;
	private float checkInterval = 1f;
	
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
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
		if (_characterState.Move.IsMoving)
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
	public new States state = States.Invisible;
	public bool turnOff = false;
	private float _baseDuration;
	private float _duration;
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{
		Debug.Log("Entering Invisible State");
		effects.Add(StatusEffect.Others);

		_characterState = character;
		_characterState.Health.SetInvincible(true);
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
			_characterState.Health.SetInvincible(false);
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
	public new States state = States.Stun;
	public bool turnOff = false;
	private PlayerAbilities _abilities;
	private float _baseDuration;
	private float _duration;
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{
		Debug.Log("Entering Stunned State");
		type = StateType.Physical;
		effects.Add(StatusEffect.Move);
		effects.Add(StatusEffect.Ability);

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
		_characterState.Move.CanMove = false;
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
			_characterState.Move.CanMove = true;
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
	public new States state = States.Desiccuration;
	public bool turnOff = false;
	private PlayerAbilities _abilities;
	private float _baseDuration;
	private float _duration;
	private float _damageToExit;
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{
		Debug.Log("Entering Desiccuration State");
		type = StateType.Physical;
		effects.Add(StatusEffect.Move);
		effects.Add(StatusEffect.Ability);

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
		_characterState.Move.CanMove = false;
		_duration = durationToExit;
		_baseDuration = durationToExit;
		//_damageToExit = damageToExit;
		_damageToExit = 0.01f;
	}

	public override void UpdateState()
	{
		Debug.Log("Updating Desiccuration State");
		_duration -= Time.deltaTime;
		if (_duration < 0 || turnOff || _characterState.Health.sumDamageTaken >= _damageToExit)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Desiccuration State");
		if (_characterState.Check(StatusEffect.Move))
		{
			_characterState.Move.CanMove = true;
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
	public new States state = States.Blind;
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
	public new States state = States.Frozen;
	public bool turnOff = false;
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
		
		_characterState.Move.CanMove = false;

		if (character.TryGetComponent<Character>(out var ability))
		{
			_abilities = ability.Abilities;
			_abilities.SetAbilitiesDisabled();
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}
		_characterState.Health.sumDamageTaken = 0;

	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_characterState.Health.sumDamageTaken >= _damageToExit || _duration < 0 || turnOff)
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
			_characterState.Move.CanMove = true;
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
		return true;
	}
}

public class FrostingState : AbstractCharacterState
{
	public new States state = States.Frosting;
	public bool turnOff = false;
	private float _duration;
	private float _baseDuration;
	private float _damageToExit;
	private PlayerAbilities _abilities;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{
		type = StateType.Magic;
		effects.Add(StatusEffect.Move);
		effects.Add(StatusEffect.AbilitySpeed);
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
		_characterState.Move.CanMove = false;

		//decrease speed
		if (character.TryGetComponent<Character>(out var ability))
		{
			_abilities = ability.Abilities;

			foreach (Ability abil in _abilities.Abilities)
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

		_characterState.Health.sumDamageTaken=0;
	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_characterState.Health.sumDamageTaken >= _damageToExit || _duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Frosting State");
		if (_characterState.Check(StatusEffect.Move))
		{
			_characterState.Move.CanMove = true;
		}
		if (_characterState.Check(StatusEffect.AbilitySpeed))
		{
			foreach (Ability abil in _abilities.Abilities)
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
	public new States state = States.Cooling;
	public bool turnOff = false;
	private float _duration;
	private float _baseDuration;
	private float _damageToExit;
	private float _curAbilityDebuf = 0.1f;
	private float _curSpeedDebuf = 0.05f;
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{
		type = StateType.Magic;
		effects.Add(StatusEffect.MoveSpeed);
		effects.Add(StatusEffect.AbilitySpeed);
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

		_characterState.Move.ChangeMoveSpeed(1-_curSpeedDebuf);
		//decrease speed of attact and movement
		_characterState.Health.sumDamageTaken = 0;
	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_characterState.Health.sumDamageTaken >= _damageToExit || _duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting cooling State");
		if (_characterState.Check(StatusEffect.MoveSpeed))
		{
			_characterState.Move.SetDefaultSpeed();
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
		_characterState.Move.ChangeMoveSpeed(1 - _curSpeedDebuf);
		//_duration = _baseDuration;
		return true;
	}

}

public class InAirState : AbstractCharacterState
{
    public new States state = States.InAir;
    private PlayerAbilities _abilities;

    private float _duration;
    private float _baseDuration;
    private float _damageToExit;

    public bool turnOff = false;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
    {
        Debug.Log("Entering InAir State");
        type = StateType.Physical;
        effects.Add(StatusEffect.Move);
        effects.Add(StatusEffect.Ability);
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

        _characterState.Move.CanMove = false;
        _duration = durationToExit;
		_baseDuration = _duration;
        _baseDuration = durationToExit;
    }

    public override void UpdateState()
    {
        Debug.Log("Updating InAir State");
        _duration -= Time.deltaTime;
		Debug.Log("_duration == " + _duration);
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }
    }


    public override void ExitState()
    {
        Debug.Log("Exiting InAir State");
        if (_characterState.Check(StatusEffect.Move))
        {
            _characterState.Move.CanMove = true;
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

public class CreeperInvisibleState : AbstractCharacterState
{
    public new States state = States.Invisible;

    private GameObject _player;

    private List<GameObject> _enemies = new List<GameObject>();

    private float lastCheckTime;
    private float checkInterval = 1f;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
    {
        Debug.Log("Entering Invisible State");
        _characterState = character;
        _player = character.gameObject;
    }

    public override void UpdateState()
    {
        Debug.Log("Updating Invisible State");
        if (_characterState.Move.IsMoving)
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
                    ExitState();
                }
            }
        }
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Invisible State");

    }

    public override bool Stack(float time)
    {
        return false;
    }
}

public class PoisonBoneState : AbstractCharacterState
{
    public new States state = States.PoisonBone;
    private PlayerAbilities _abilities;

    private int _currentStacks = 0;
    private int _maxStacks = 4;

    private float _baseDamage = 1f;
	private float _endDamage;

    private float _timeBetweenAttack;
    private float _startTimeBetweenAttack = 1f;

    private float _duration;
    private float _baseDuration;

    public bool turnOff = false;
	public int CurrentStacks { get => _currentStacks; set => _currentStacks = value; }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
    {
		Debug.Log("Яд костей дебафф сработал. Вход в состояние");
        _timeBetweenAttack = _startTimeBetweenAttack;

        type = StateType.Physical;
        effects.Add(StatusEffect.Ability);

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

        _duration = durationToExit;
        _baseDuration = durationToExit;

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }
    }

    public override void UpdateState()
    {
        _timeBetweenAttack -= Time.deltaTime;
        //Debug.Log("_timeBetweenAttack == " + _timeBetweenAttack);
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
        if (_characterState.Check(StatusEffect.Ability) && _abilities != null)
        {
            _abilities.SetAbilitiesEnabled();
        }

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

	private void DamageDeal()
	{
		_endDamage = _currentStacks * _baseDamage;

		_characterState.Health.TryTakeDamage(_endDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
	}

    private void ResetValues()
    {
        _currentStacks = 0;
        _baseDuration = 0;
        _duration = 0;
        _endDamage = 0;
        _baseDamage = 1f;
    }
}

public class PoisonCloudState : AbstractCharacterState
{
    public new States state = States.PoisonCloud;
    private PlayerAbilities _abilities;
    private ToxiqueCloud _toxiqueCloud;

    private float _currentStacks = 0;
    private float _maxStacks = 5;
	private float _radiusCloud = 3.5f;

	private float _baseDamage = 0.005f;
	private float _increasedDamage;
	private float _endDamage;

	private float _timeBetweenAttack;
	private float _startTimeBetweenAttack = 1f;

    private float _duration;
    private float _baseDuration;
	private float _durationEmpathicPoisons = 3f;

    private float _damageToExit;

    public bool turnOff = false;
	public Character Player;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
    {
		_timeBetweenAttack = _startTimeBetweenAttack;

        type = StateType.Physical;
        effects.Add(StatusEffect.Ability);

        _characterState = character;
        _toxiqueCloud = _characterState.GetComponentInChildren<ToxiqueCloud>();
		Player = _characterState.GetComponent<Character>();

        if (character.TryGetComponent<Character>(out var ability))
        {
            _abilities = ability.Abilities;
            _abilities.SetAbilitiesDisabled();
        }
        else
        {
            Debug.Log("no ability at " + character.gameObject.name);
        }

        _duration = durationToExit;
        _baseDuration = durationToExit;

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }
    }

    public override void UpdateState()
    {
        _timeBetweenAttack -= Time.deltaTime;
		//Debug.Log("_timeBetweenAttack == " + _timeBetweenAttack);
		if (_timeBetweenAttack <= 0)
		{
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
        if (_characterState.Check(StatusEffect.Ability) && _abilities != null)
        {
            _abilities.SetAbilitiesEnabled();
        }

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

	private void SearchingEnemies()
	{
		Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(_characterState.transform.position, _radiusCloud);
		foreach (Collider2D enemy in hitEnemies)
		{
			if (enemy.CompareTag("Enemies"))
			{
				if (enemy.TryGetComponent<HealthComponent>(out var target) && enemy.transform != _characterState.transform)
				{
					DamageDeal(target);
					_timeBetweenAttack = _startTimeBetweenAttack;

				}
			}
		}
	}

	private void DamageDeal(HealthComponent targetHealth)
	{
		_increasedDamage = _baseDamage * _currentStacks;
		_endDamage = targetHealth.MaxHealth * _increasedDamage;
		targetHealth.TryTakeDamage(_endDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
		if (_toxiqueCloud.isActive)
		{
			//var empathicPoisons = EmpathicPoisons.Intstance;
			targetHealth.GetComponent<CharacterState>().AddState(new EmpathicPoisonsState(), _durationEmpathicPoisons, 10000, States.EmpathicPoisons);
			EmpathicPoisonsState.Player = Player.gameObject;
			EmpathicPoisonsState.RadiusCloud = _radiusCloud;
        }
	}

    private void ResetValues()
    {
        _currentStacks = 0;
        _baseDuration = 0;
        _duration = 0;
        _endDamage = 0;
        _increasedDamage = 0;
        _baseDamage = 0.5f;
    }
}

public class EmpathicPoisonsState : AbstractCharacterState
{
	public new States state = States.EmpathicPoisons;

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

	private HealthComponent _characterHealth;
	private Vector3 _playerPosition;
	private Vector3 _characterPosition;

	private bool _isInPoisonCloud;
	public bool turnOff = false;

	public static float RadiusCloud;

	public static GameObject Player;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{
		_timeBetweenToApplyStacks = _startTimeBetweentoApplyStacks;

		type = StateType.Physical;
		effects.Add(StatusEffect.Ability);

		_characterState = character;
		_characterHealth = _characterState.GetComponent<HealthComponent>();
		_originalEvasionValue = _characterHealth.EvadeMeleeDamage;

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

        _characterHealth.EvadeMeleeDamage = _originalEvasionValue - _endEvasionValue;
	}

    private void DecreaseEvasionForCurrentTarget()
    {
        float reductionPerSecond = _baseEvasionValue * 0.33f;
        _endEvasionValue = Mathf.Max(_originalEvasionValue, _characterHealth.EvadeMeleeDamage + reductionPerSecond);
        _characterHealth.EvadeMeleeDamage = _endEvasionValue;
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
		_characterHealth.EvadeMeleeDamage = _originalEvasionValue;
    }
}

public class AbilitySchoolDebuff : AbstractCharacterState
{
	public new States state = States.SchoolDebuff;
	public bool turnOff = false;
	private PlayerAbilities _abilities;
	private float _baseDuration;
	private float _duration;
	public Schools canceledSchoool;
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{
		Debug.Log("Entering AbilitySchoolDebuff State");
		type = StateType.Immaterial;
		effects.Add(StatusEffect.AbilitySchool);

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
	public new States state = States.FormDebuf;
	public bool turnOff = false;
	private PlayerAbilities _abilities;
	private float _baseDuration;
	private float _duration;
	public AbilityForm canceledForm;
	public bool canCancel = false;
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit)
	{
		Debug.Log("Entering AbilityFormDebuff State");
		type = StateType.Immaterial;
		effects.Add(StatusEffect.AbilitySchool);

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

public class CharacterState : NetworkBehaviour
{
	private HealthComponent _health;
	private MoveComponent _move;
	private StaminaComponent _stamina;
	private List<AbstractCharacterState> currentStates = new List<AbstractCharacterState>();
	[SerializeField] private StateIcons _stateIcons;
	public bool invinsible = false;
	public HealthComponent Health => _health;
	public MoveComponent Move => _move;
	public StaminaComponent Stamina => _stamina;

	public Dictionary<States, AbstractCharacterState> enumToState = new Dictionary<States, AbstractCharacterState>()
	{
		[States.Stun] = new StunnedState(),
		[States.Frozen] = new FrozenState(),
		[States.Frosting] = new FrostingState(),
		[States.Cooling] = new Cooling(),
		[States.Blind] = new BlindnessState(),
		[States.PoisonBone] = new PoisonBoneState(),
		[States.PoisonCloud] = new PoisonCloudState(),
		[States.EmpathicPoisons] = new EmpathicPoisonsState(),
		[States.Invisible] = new InvisibleState(),
		[States.SchoolDebuff] = new AbilitySchoolDebuff(),
		[States.Desiccuration] = new Desiccuration()
	};

	public void Initialize(HealthComponent health, MoveComponent move, StaminaComponent stamina)
	{
		_health = health;
		_move = move;
		_stamina = stamina;
		if (_move == null || _health == null || _stamina == null)
		{
			Debug.LogError("No required component in " + gameObject.name);
		}
	}

	private void Update()
	{
		if (currentStates.Count > 0)
		{
			for (int i = 0; i < currentStates.Count; i++)
			{
				currentStates[i].UpdateState();
			}
		}

		if(Input.GetKeyDown(KeyCode.R))
		{
			CmdAddState(States.Stun, 10, 0);
		}
	}

	public void AddState(AbstractCharacterState newState, float duration, float damageToExit, States state)
	{
		if (invinsible)
			return;
		if (CheckForState(state))
		{
			foreach (AbstractCharacterState item in currentStates)
			{
				if (item.state != state) continue;

				if (item.Stack(duration))
				{
					//_stateIcons.ActivateIco(state, duration, 1);
				}
				else
				{
					//nothing at this time??
				}
			}
		}
		else
		{
			_stateIcons.ActivateIco(state, duration, 1);
			currentStates.Add(newState);
			currentStates[currentStates.Count - 1].state = state;
			//currentStates[currentStates.Count - 1].
			currentStates[currentStates.Count - 1].EnterState(this, duration, damageToExit);
		}
	}

	public void AddState(AbstractCharacterState newState, float duration, float damageToExit, States state, Schools schools)
	{
		//if already exist 
		//if (currentStates.Contains(newState))
		if (CheckForState(state))
		{
			foreach (AbstractCharacterState item in currentStates)
			{
				if (item.state != state) continue;

				if (item.Stack(duration))
				{
					_stateIcons.ActivateIco(state, duration, 1);
				}
				else
				{
					_stateIcons.ActivateIco(state, duration, 1);
					currentStates.Add(newState);
					var counterSpell = (AbilitySchoolDebuff)newState;
					counterSpell.canceledSchoool = schools;
					currentStates[currentStates.Count - 1].state = state;
					//currentStates[currentStates.Count - 1].
					currentStates[currentStates.Count - 1].EnterState(this, duration, damageToExit);
				}
			}
		}
		else
		{
			_stateIcons.ActivateIco(state, duration, 1);
			currentStates.Add(newState);
			var counterSpell = (AbilitySchoolDebuff)newState;
			counterSpell.canceledSchoool = schools;
			currentStates[currentStates.Count - 1].state = state;
			//currentStates[currentStates.Count - 1].
			currentStates[currentStates.Count - 1].EnterState(this, duration, damageToExit);
		}
	}
	//can Cancel- if debuf can cancel ability that is casting right now
	public void AddState(AbstractCharacterState newState, float duration, float damageToExit, States state, AbilityForm form, bool canCancel)
	{
		//if already exist 
		//if (currentStates.Contains(newState))
		if (CheckForState(state))
		{
			foreach (AbstractCharacterState item in currentStates)
			{
				if (item.state != state) continue;

				if (item.Stack(duration))
				{
					_stateIcons.ActivateIco(state, duration, 1);
				}
				else
				{
					_stateIcons.ActivateIco(state, duration, 1);
					currentStates.Add(newState);
					var counterSpell = (AbilityFormDebuff)newState;
					counterSpell.canCancel = canCancel;
					counterSpell.canceledForm = form;
					currentStates[currentStates.Count - 1].state = state;
					//currentStates[currentStates.Count - 1].
					currentStates[currentStates.Count - 1].EnterState(this, duration, damageToExit);
					//nothing at this time??
				}
			}
		}
		else
		{
			_stateIcons.ActivateIco(state, duration, 1);
			currentStates.Add(newState);
			var counterSpell = (AbilityFormDebuff)newState;
			counterSpell.canCancel = canCancel;
			counterSpell.canceledForm = form;
			currentStates[currentStates.Count - 1].state = state;
			//currentStates[currentStates.Count - 1].
			currentStates[currentStates.Count - 1].EnterState(this, duration, damageToExit);
		}
	}

	[Command]
	public void CmdAddState(States state, float duration, float damageToExit, Schools schools)
	{
		AddStateLogic(state, duration, damageToExit, schools);
		ClientAddState(state, duration, damageToExit, schools);
	}

	//[Command]
	public void CmdAddState(States state, float duration, float damageToExit)
	{
		Debug.Log("Add state cmd");
		AddStateLogic(state, duration, damageToExit, Schools.None);
		ClientAddState(state, duration, damageToExit, Schools.None);
	}

	/*public void AddNewState(States state, float duration, float damageToExit)
	{
		CmdAddState(state, duration, damageToExit);
		//ClientAddState(state, duration, damageToExit, Schools.None);
	}*/

	[ClientRpc]
	private void ClientAddState(States state, float duration, float damageToExit, Schools schools)
	{
		Debug.Log("Add state rpc");
		AddStateLogic(state, duration, damageToExit, schools);
	}

	public bool IfHasState(AbstractCharacterState newState)
	{
		//ITS NOT WORKING!!!!
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

	public void RemoveState(States stateName)
	{
		foreach (AbstractCharacterState state in currentStates)
		{
			if (state.state == stateName)
			{
				state.ExitState();
			}
		}
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

	public bool CheckForState(States state)
	{
		foreach (AbstractCharacterState states in currentStates)
		{
			if(states.state == state)
			{
				return true;
			}
		}
		return false;
	}

	private void AddStateLogic(States state, float duration, float damageToExit, Schools school)
	{
		Debug.Log("Add state logic");
		if (invinsible)
			return;
		if (CheckForState(state))
		{
			foreach (AbstractCharacterState item in currentStates)
			{
				if (item.state != state) continue;

				if (item.Stack(duration))
				{
					_stateIcons.ActivateIco(state, duration, 1);
				}
				else
				{
					//nothing at this time??
				}
			}
		}
		else
		{
			CreateState(enumToState[state], state, duration, damageToExit);

			if(school!=Schools.None)
			{
				var counterSpell = (AbilitySchoolDebuff)enumToState[state];
				counterSpell.canceledSchoool = school;
			}
		}
	}

	private void CreateState(AbstractCharacterState state, States stateName, float duration, float damageToExit)
	{
		_stateIcons.ActivateIco(stateName, duration, 1);
		currentStates.Add(state);
		currentStates[currentStates.Count - 1].EnterState(this, duration, damageToExit);
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
	Default,
	Stun,
	Frozen,
	Frosting,
	Cooling,
	InAir,
	PoisonBone,
	PoisonCloud,
	EmpathicPoisons,
	Blind,
	Invisible,
	SchoolDebuff,
	FormDebuf,
	Desiccuration
}

