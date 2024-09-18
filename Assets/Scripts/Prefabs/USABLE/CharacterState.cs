using System;
using System.Collections;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

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
		_characterState.invincible = true;
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
			_characterState.invincible = false;
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
	//private PlayerAbilities _abilities;
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
		return true;
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

			foreach (var abil in _abilities.Abilities)
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
			foreach (var abil in _abilities.Abilities)
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
	//private PlayerAbilities _abilities;
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

public class SpiritEnergyState : AbstractCharacterState
{
    private float _baseDuration;
    private float _duration;
    private int _stacks;
    private const int MaxStacks = 2;
    private const float ManaRestorePerStack = 0.09f;
    private const float ShieldStrengthIncreaseFirstStack = 0.10f;
    private const float ShieldStrengthIncreaseSecondStack = 0.05f;

    private List<StatusEffect> _effects = new ();

    public override States State => States.SpiritEnergy;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _duration = durationToExit;
        _baseDuration = durationToExit;
        _stacks = 1;

        ApplyManaRestore();
        ApplyShieldStrengthIncrease();
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;

        if (_duration <= 0 || _stacks == 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        ResetShieldStrength();

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (_stacks >= MaxStacks)
        {
            return false;
        }

        _stacks++;
        _duration = Mathf.Max(_duration, time);

        ApplyManaRestore();
        ApplyShieldStrengthIncrease();

        return true;
    }

    private void ApplyManaRestore()
    {
        _characterState.Character.Resources.FirstOrDefault(o => o.Type == ResourceType.Mana)?.Add(ManaRestorePerStack * _stacks);
    }

    private void ApplyShieldStrengthIncrease()
    {
        if (_stacks == 1)
        {
	        
        }
        else if (_stacks == 2)
        {
	        
        }
    }

    private void ResetShieldStrength()
    {
        // Reset shield strength to its original value if applicable
    }
}

public class SpiritHealthState : AbstractCharacterState
{
    private float _baseDuration;
    private float _duration;
    private int _stacks;
    private const int MaxStacks = 2;
    private const float HealthRestorePerStack = 0.09f; // 9% health restore per stack
    private const float ManaRestorePerStack = 0.09f; // 9% mana restore per stack

    private List<StatusEffect> _effects = new ();

    public override States State => States.SpiritHealth;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _duration = durationToExit;
        _baseDuration = durationToExit;
        _stacks = 1;
        
        ApplyManaRestore();
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;

        if (_duration <= 0 || _stacks == 0)
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
        if (_stacks >= MaxStacks)
        {
            return false;
        }

        _stacks++;
        _duration = Mathf.Max(_duration, time);

        ApplyManaRestore();

        return true;
    }

    private void ApplyManaRestore()
    {
        _characterState.Character.Resources.FirstOrDefault(o=>o.Type == ResourceType.Mana)?.Add(ManaRestorePerStack * _stacks);
    }
}

public class TiredSoul : AbstractCharacterState
{
	private float _duration;

	public override States State => States.TiredSoul;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => new List<StatusEffect>();

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_duration = durationToExit;
		Debug.Log("TiredSoul debuff applied to " + character.name);
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
		Debug.Log("TiredSoul debuff expired.");
		_characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		_duration += time;
		return true;
	}
}

public class LightShield : AbstractCharacterState, IDamageable
{
	private float _damageAbsorbed;
	private float _maxAbsorption;
	private float _duration;

	public event Action<float, DamageType> DamageTaken;

	public override States State => States.LightShield;
	public override StateType Type => StateType.Immaterial;
	public override List<StatusEffect> Effects => new List<StatusEffect>();

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_duration = durationToExit;
		_maxAbsorption = damageToExit;
		_damageAbsorbed = 0;
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
		Debug.Log("LightShield state exited.");
		_characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		_duration = time;
		_damageAbsorbed = 0;
		return true;
	}

	public bool TryTakeDamage(ref Damage damage, Skill skill)
	{
		float damageToAbsorb = Mathf.Min(_maxAbsorption - _damageAbsorbed, damage.Value);
		_damageAbsorbed += damageToAbsorb;
		damage.Value -= damageToAbsorb;

		DamageTaken?.Invoke(damageToAbsorb, damage.Type);

		if (_damageAbsorbed >= _maxAbsorption)
		{
			ExitState();
			return true;
		}

		return damage.Value == 0;
	}
}

public class DarkShield : AbstractCharacterState
{
    private float _damageDebuffDelay = 0.2f;
    private float _maxDamagePerTick;
    private float _duration;
    private Health _healthComponent;

    public override States State => States.DarkShield;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _duration = durationToExit;
        _maxDamagePerTick = damageToExit;
        
        _healthComponent = character.GetComponent<Health>();
        if (_healthComponent != null)
        {
            _healthComponent.DamageTaken += HandleDamageTaken;
        }
    }

    public override void ExitState()
    {
        if (_healthComponent != null)
        {
            _healthComponent.DamageTaken -= HandleDamageTaken;
        }
        
        _characterState.RemoveState(this);
    }

    private void HandleDamageTaken(float damage, DamageType type)
    {
        if (_healthComponent == null) return;
        
        _healthComponent.StartCoroutine(ApplyDelayedDamage(damage));
    }

    private IEnumerator ApplyDelayedDamage(float damage)
    {
        yield return new WaitForSeconds(_damageDebuffDelay);

        var damageToApply = Mathf.Min(damage, _maxDamagePerTick);
        var damageToTake = new Damage { Value = damageToApply };
        
        _healthComponent.TryTakeDamage(ref damageToTake, null);
    }

    public override bool Stack(float time)
    {
        _duration = time;
        return true;
    }

    public override void UpdateState()
    {
	    _duration -= Time.deltaTime;
	    if (_duration <= 0)
	    {
		    ExitState();
	    }
    }
}

public class ReversePolarityState : AbstractCharacterState
{
	public override States State => States.ReversePolarity;
	public override StateType Type => StateType.Immaterial;
	public override List<StatusEffect> Effects => new List<StatusEffect>();

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
	}

	public override void UpdateState()
	{
	}

	public override void ExitState()
	{
		_characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		return false;
	}
}

public class CharacterState : NetworkBehaviour
{
    private Character _hero;
    private List<AbstractCharacterState> currentStates = new List<AbstractCharacterState>();
    [SerializeField] private StateIcons _stateIcons;

    public bool invincible = false;
    public Character Character => _hero;

    private Dictionary<States, AbstractCharacterState> enumToState = new Dictionary<States, AbstractCharacterState>()
    {
        [States.Stun] = new StunnedState(),
        [States.Frozen] = new FrozenState(),
        [States.Frosting] = new FrostingState(),
        [States.Cooling] = new Cooling(),
        [States.Blind] = new BlindnessState(),
        [States.Invisible] = new InvisibleState(),
        [States.SchoolDebuff] = new AbilitySchoolDebuff(),
        [States.Desiccuration] = new Desiccuration(),
        [States.Plague] = new Plague(),
        [States.Curse] = new Curse(),
        [States.NorthernerEndurance] = new NorthernerEndurance(),
        [States.LastBreath] = new LastBreath(),
        [States.MagicBuff] = new MagicBuff(),
        [States.SpiritEnergy] = new SpiritEnergyState(),
        [States.SpiritHealth] = new SpiritHealthState(),
        [States.TiredSoul] = new TiredSoul(),
        [States.LightShield] = new LightShield(),
        [States.DarkShield] = new DarkShield(),
        [States.ReversePolarity] = new ReversePolarityState()
    };

    public void Initialize(Character hero)
    {
        _hero = hero;
        if (_hero == null)
        {
            Debug.LogError("No required component in " + name + " " + gameObject.name);
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
    }

    public void Dispel(StateType type)
    {
        foreach (AbstractCharacterState state in currentStates)
        {
            if (state.Type == type)
            {
                state.ExitState();
            }
        }
    }

    public bool Check(StatusEffect effect)
    {
        foreach (AbstractCharacterState state in currentStates)
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
        foreach (AbstractCharacterState states in currentStates)
        {
            if (states.State == state)
            {
                return true;
            }
        }
        return false;
    }

    public AbstractCharacterState GetState(States state)
    {
        foreach (AbstractCharacterState states in currentStates)
        {
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
        AddStateLogic(state, duration, damageToExit, Schools.None, personWhoShooted, skillName);
        ClientAddState(state, duration, damageToExit, Schools.None, personWhoShooted, skillName);
    }

    public void AddState(States state, float duration, float damageToExit, GameObject personWhoShooted, string skillName)
    {
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
        if (currentStates.Contains(newState))
        {
            currentStates.Remove(newState);
        }
    }

    private void RemoveStateLogic(States stateName)
    {
        if (currentStates.Count <= 0) return;

        _stateIcons.RemoveItemByState(stateName);
        for (int i = currentStates.Count - 1; i >= 0; i--)
        {
            if (currentStates[i].State == stateName)
            {
                currentStates[i].ExitState();
                if (currentStates[i] is IDamageable damageableShield)
                {
                    RemoveShield(damageableShield);
                }
                currentStates.RemoveAt(i);
            }
        }
    }

    [ClientRpc]
    private void ClientAddState(States state, float duration, float damageToExit, Schools schools, GameObject personWhoShooted, string skillName)
    {
        AddStateLogic(state, duration, damageToExit, schools, personWhoShooted, skillName);
    }

    [ClientRpc]
    private void ClientRemoveState(States stateName)
    {
        RemoveStateLogic(stateName);
    }

    private void AddStateLogic(States state, float duration, float damageToExit, Schools school, GameObject personWhoShooted, string skillName)
    {
        if (invincible) return;

        if (CheckForState(state))
        {
            for (int i = 0; i < currentStates.Count; i++)
            {
                if (currentStates[i].State == state)
                {
                    if (currentStates[i].Stack(duration))
                    {
                        _stateIcons.ActivateIco(state, duration, 1, true);
                    }
                    else
                    {
                        CreateState(enumToState[state], state, duration, damageToExit, personWhoShooted, skillName, false);
                    }
                    break;
                }
            }
        }
        else
        {
            CreateState(enumToState[state], state, duration, damageToExit, personWhoShooted, skillName, false);
            if (enumToState[state] is IDamageable damageableShield)
            {
                AddShield(damageableShield);
            }

            if (school != Schools.None)
            {
                var counterSpell = (AbilitySchoolDebuff)enumToState[state];
                counterSpell.canceledSchoool = school;
            }
        }
    }

    private void CreateState(AbstractCharacterState state, States stateName, float duration, float damageToExit, GameObject personWhoShooted, string skillName, bool stack)
    {
        _stateIcons.ActivateIco(stateName, duration, 1, stack);
        currentStates.Add(state);

        if (personWhoShooted.TryGetComponent<Character>(out var character))
        {
            currentStates[^1].EnterState(this, duration, damageToExit, character, skillName);
        }
        else
        {
            currentStates[^1].EnterState(this, duration, damageToExit, null, skillName);
        }
    }

    private void AddShield(IDamageable shield)
    {
        var health = _hero.GetComponent<Health>();
        if (health != null)
        {
            health.Shields.Add(shield);
        }
    }

    private void RemoveShield(IDamageable shield)
    {
        var health = _hero.GetComponent<Health>();
        if (health != null)
        {
            health.Shields.Remove(shield);
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
	Default,
	Stun,
	Frozen,
	Frosting,
	Cooling,
	Blind,
	Invisible,
	SchoolDebuff,
	FormDebuf,
	Desiccuration,
	Plague,
	Curse,
	NorthernerEndurance,
	LastBreath,
	MagicBuff,
	PoisonBone,
	WitheringPoison,
	InAir,
	PoisonCloud,
	CreeperInvisible,
	InstantHealingPoison,
	RegeneratingPoison,
	HealingPoisonPerSecond,
	SpiritEnergy,
	SpiritHealth,
	TiredSoul,
	LightShield,
	DarkShield,
	ReversePolarity
}

