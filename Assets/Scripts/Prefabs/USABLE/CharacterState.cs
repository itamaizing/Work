using System;
using System.Collections;
using Mirror;
using System.Collections.Generic;
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
/*
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
*/

public class SpiritEnergyState : AbstractCharacterState
{
	private Character _hero;
	private List<AbstractCharacterState> currentStates = new List<AbstractCharacterState>();
	[SerializeField] private StateIcons _stateIcons;

	public bool invinsible = false;

	public Character Character => _hero;

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
		_characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		return false;
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
*/

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

	[Command]
	public void CmdRemoveState(States state)
	{
		Debug.Log("Remove state" + state);
		RemoveStateLogic(state);
		ClientRemoveState(state);
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

	private void RemoveStateLogic(States stateName)
	{
		Debug.Log("Remove state logic" + stateName);
		if (currentStates.Count <= 0) return;

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

	[ClientRpc]
	private void ClientRemoveState(States stateName)
	{
		Debug.Log("Remove state client" + stateName);
		RemoveStateLogic(stateName);
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

