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
	public abstract float CurrentValue { get; set; }

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

    public override float CurrentValue { get; set; }

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
	private List<AbstractCharacterState> _currentStates = new List<AbstractCharacterState>();
	[SerializeField] private StateIcons _stateIcons;

	public bool invinsible = false;

	public Character Character => _hero;

	public List<AbstractCharacterState> CurrentStates { get => _currentStates; }
	public Dictionary<States, AbstractCharacterState> enumToState = new Dictionary<States, AbstractCharacterState>()
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
		[States.AbsorptionOfPoison] = new AbsorptionOfPoisonsState(),
		#endregion

		#region Carrigan
		[States.Bleeding] = new BleedingState(),
		[States.ReducingHealing] = new ReducingHealingState(),
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
			//Debug.LogError("No required component in " + name + " " + gameObject.name);
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

	public void DispelAllState(StateType type)
	{
        foreach (AbstractCharacterState state in _currentStates)
        {
            if (state.Type == type)
            {
                state.ExitState();
            }
        }
    }

	public void DispelOneState(StateType type)
	{
		foreach (AbstractCharacterState state in _currentStates)
		{
			if (state.Type == type)
			{
				state.ExitState();
				break;
			}
		}
	}

	public bool TEST_CheckStateType(StateType type)
	{
        foreach (AbstractCharacterState state in _currentStates)
        {
            if (state.Type == type)
            {
                return true;
            }
        }
		return false;
    }

	public List<AbstractCharacterState> TEST_GetStatesOnEffectAndType(StatusEffect effect, StateType type)
	{
		List<AbstractCharacterState> currentStates = new();

		if (Check(effect) && TEST_CheckStateType(type))
		{
			foreach (AbstractCharacterState state in _currentStates)
			{
				if (state.Effects.Contains(effect) && state.Type == type)
				{
					currentStates.Add(state);
				}
			}

			if (currentStates.Count > 0)
			{
				return currentStates;
            }
			else
			{
				return null;
			}
		}
		else
		{
            return null;
		}
	}

	public bool Check(StatusEffect effect)
	{
		foreach (AbstractCharacterState state in _currentStates)
		{
			if (state.Effects.Contains(effect))
            {
                //Debug.Log("StatusEffect on Target = " + effect);
                return true;
			}
		}
		return false;
	}

	public bool CheckForState(States state)
	{
		foreach (AbstractCharacterState states in _currentStates)
		{
			//Debug.Log(states.State + " on enemy, check for " + state);
			if (states.State == state)
			{
                return true;
			}
		}
		return false;
	}

	public bool CheckPoisonStates()
	{
		var poisonStates = new List<States>
		{
			States.PoisonBone,
			States.WitheringPoison,
			States.BindingPoison,
			States.PoisonCloud
		};

        foreach (AbstractCharacterState state in _currentStates)
        {
            if (poisonStates.Contains(state.State))
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
			//Debug.Log(states.State + " on enemy, check for " + state);
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
		//Debug.Log("Add state from server");
		AddStateLogic(state, duration, damageToExit, Schools.None, personWhoShooted, skillName);
		ClientAddState(state, duration, damageToExit, Schools.None, personWhoShooted, skillName);
    }
    public void AddStateTest(States state, float duration, float damageToExit, GameObject personWhoShooted, string skillName)
    {
        //Debug.Log("Add state from server");
        AddStateLogic(state, duration, damageToExit, Schools.None, personWhoShooted, skillName);
    }

    [Command]
	public void CmdRemoveState(States state)
	{
		//Debug.Log("Remove state" + state);
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
		//Debug.Log("Remove state logic" + stateName);
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
		//Debug.Log("Add state rpc");
		AddStateLogic(state, duration, damageToExit, schools, personWhoShooted, skillName);
	}

	[ClientRpc]
	private void ClientRemoveState(States stateName)
	{
		//Debug.Log("Remove state client" + stateName);
		RemoveStateLogic(stateName);
	}

	private void AddStateLogic(States state, float duration, float damageToExit, Schools school, GameObject personWhoShooted, string skillName)
	{
		Debug.Log("Add state logic");
		if (invinsible)
			return;
		if (CheckForState(state))
		{
			for(int i = 0; i < _currentStates.Count; i++)
			{
				if (_currentStates[i].State != state) continue;

				if (_currentStates[i].Stack(duration))
				{
					_stateIcons.ActivateIco(state, duration, 1, true);
				}
				else
				{
					CreateState(enumToState[state], state, duration, damageToExit, personWhoShooted, skillName, false);
					break;
					//nothing at this time??
				}
			}
		}
		else
		{
			CreateState(enumToState[state], state, duration, damageToExit, personWhoShooted, skillName, false);

			if(school!=Schools.None)
			{
				var counterSpell = (AbilitySchoolDebuff)enumToState[state];
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
    Others,
	Move,
	MoveSpeed,
	Ability,
	AbilitySchool,
	AbilitySpeed,
    Absorptions,
    Poison,
    Healing,
    Freezing,
    Stunning,
    Invisible,
    Strengthening, // For all State increasing/reduction Health/Mana/other values
    Immateriality,
    ReducingEfficiency,
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
	AbsorptionOfPoison,

    #endregion

    #region Carrigan
	Bleeding,
	ReducingHealing,
    #endregion

    Immateriality,
    InAir,
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
	MagicBuff
}

