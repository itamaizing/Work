using Mirror;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractCharacterState
{
	protected CharacterState _characterState;
	protected SkillManager _abilities;
	protected Health _health;
	protected Character _personWhoMadeBuff;

	public int CurrentStacksCount = 0;
	public int MaxStacksCount = 0;

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

public class CharacterState : NetworkBehaviour
{
	private Character _hero;
	private List<AbstractCharacterState> currentStates = new List<AbstractCharacterState>();
	[SerializeField] private StateIcons _stateIcons;

	public bool invinsible = false;

	public List<AbstractCharacterState> CurrentStates => currentStates;
	public Character Character => _hero;

	public Dictionary<States, AbstractCharacterState> enumToState = new Dictionary<States, AbstractCharacterState>()
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
		[States.DarkShield] = new DarkShield(),
		[States.LightShield] = new LightShield(),
		[States.TiredSoul] = new TiredSoul(),
		[States.ReversePolarity] = new ReversePolarityState(),
		[States.SpiritEnergy] = new SpiritEnergyState(),
		[States.SpiritHealth] = new SpiritHealthState(),
		[States.ScorchedSoul] = new ScorchedSoul(),
		[States.Knockdown] = new Knockdown(),
		[States.IdealEvade] = new IdealEvade(),
		[States.Bleeding] = new BleedingDebuff(),
		[States.EmeraldSkin] = new EmeraldSkinState(),
		[States.DefenseReduction] = new DefenceReductionState(),
		[States.SparkTalentHealthBuff] = new SparkTalentHealthState(),
		[States.SelfHarm] = new SelfHarmState()
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
			Debug.Log(states.State + " on enemy, check for " + state);
			if (states.State == state)
			{
				return true;
			}
		}
		return false;
	}
	
	public int CheckStateStacks(States state)
	{
		foreach (AbstractCharacterState states in currentStates)
		{
			Debug.Log(states.State + " on enemy, check for " + state);
			if (states.State == state)
			{
				return states.CurrentStacksCount;
			}
		}
		return 0;
	}

	public AbstractCharacterState GetState(States state)
	{
		foreach (AbstractCharacterState states in currentStates)
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
		Debug.Log("Remove state" + state);
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
		if (!currentStates.Contains(newState)) return;
		
		if (newState is IDamageable damageableShield)
		{
			RemoveShield(damageableShield);
		}
			
		currentStates.Remove(newState);
	}

	private void RemoveStateLogic(States stateName)
	{
		if (currentStates.Count <= 0) return;

		_stateIcons.RemoveItemByState(stateName);
		for (int i = currentStates.Count - 1; i >= 0; i--)
		{
			if (currentStates[i].State == stateName)
			{
				if (currentStates[i] is IDamageable damageableShield)
				{
					RemoveShield(damageableShield);
				}
				
				currentStates[i].ExitState();
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
		Debug.Log("Remove state client" + stateName);
		RemoveStateLogic(stateName);
	}

	private void AddStateLogic(States state, float duration, float damageToExit, Schools school,
		GameObject personWhoShooted, string skillName)
	{
		if (invinsible) return;

		Debug.Log(state);

		if (CheckForState(state))
		{
			for (int i = 0; i < currentStates.Count; i++)
			{
				if (currentStates[i].State == state)
				{
					if (currentStates[i].CurrentStacksCount < currentStates[i].MaxStacksCount)
					{
						var canStack = currentStates[i].Stack(duration);
						_stateIcons.ActivateIco(state, duration, 1, canStack);
					}
					else if(currentStates[i].MaxStacksCount == 0 || currentStates[i].CurrentStacksCount == currentStates[i].MaxStacksCount )
					{
						var canStack = currentStates[i].Stack(duration); 
						_stateIcons.ActivateIco(state, duration, 0, canStack);
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
			currentStates[currentStates.Count - 1].EnterState(this, duration, damageToExit, character, skillName);
		}
		else
		{
			currentStates[currentStates.Count - 1].EnterState(this, duration, damageToExit, null, skillName);
		}
	}
	
	private void AddShield(IDamageable shield)
	{
		var health = _hero.GetComponent<Health>();
		if (health != null)
		{
			Debug.Log("Add Shield By " + shield);
			health.Shields.Add(shield);
		}
	}

	private void RemoveShield(IDamageable shield)
	{
		var health = _hero.GetComponent<Health>();
		if (health != null)
		{
			Debug.Log("Remove Shield By " + shield);
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
	DarkShield,
	LightShield,
	ReversePolarity,
	SpiritEnergy,
	SpiritHealth,
	TiredSoul,
	ScorchedSoul,
	Knockdown,
	IdealEvade,
	Bleeding,

	EmeraldSkin,
	SparkTalentHealthBuff,
	DefenseReduction,
	SelfHarm
}