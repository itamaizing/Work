using Mirror;
using System.Collections.Generic;
using System.Linq;
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
	public abstract BaffDebaff BaffDebaff { get; }
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
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
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

public abstract class HealStates : AbstractCharacterState
{
	public float HealingValue { get; set; }
}

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
		[States.SelfHarm] = new SelfHarmState(),
		[States.Immateriality] = new ImmaterialityState(),
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
		[States.Bleeding] = new BleedingState(),
		[States.ReducingHealing] = new ReducingHealingState(),
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
				return true;
			}
		}
		return false;
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
	public bool CheckStateType(StateType type)
	{
		foreach (AbstractCharacterState state in currentStates)
		{
			if (state.Type == type)
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
		//Debug.Log("Add state cmd");
		AddStateLogic(state, duration, damageToExit, Schools.None, personWhoShooted, skillName);
		ClientAddState(state, duration, damageToExit, Schools.None, personWhoShooted, skillName);
	}

	public void AddState(States state, float duration, float damageToExit, GameObject personWhoShooted, string skillName)
	{
		//Debug.Log("Add state from server");
		AddStateLogic(state, duration, damageToExit, Schools.None, personWhoShooted, skillName);
		ClientAddState(state, duration, damageToExit, Schools.None, personWhoShooted, skillName);
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
		if (!currentStates.Contains(newState)) return;

		if (newState is IDamageable damageableShield)
		{
			RemoveShield(damageableShield);
		}
		_stateIcons.RemoveItemByState(newState.State);
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
				currentStates.Remove(currentStates[i]);

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

	/*private void AddStateLogic(States state, float duration, float damageToExit, Schools school,
		GameObject personWhoShooted, string skillName)
	{
		if (invinsible) return;

		//Debug.Log(state);

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
					else if (currentStates[i].MaxStacksCount == 0 || currentStates[i].CurrentStacksCount == currentStates[i].MaxStacksCount)
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
	}*/


	private void AddStateLogic(States state, float duration, float damageToExit, Schools school, GameObject personWhoShooted, string skillName, bool isCanDodgeMagState = false)
	{
		if (invinsible) return;

		// Если состояние уже есть, добавляем стаки и перемещаем в конец списка
		for (int i = 0; i < currentStates.Count; i++)
		{
			if (currentStates[i].State == state)
			{
				if (currentStates[i].CurrentStacksCount < currentStates[i].MaxStacksCount)
				{
					var canStack = currentStates[i].Stack(duration);
					_stateIcons.ActivateIco(state, duration, 1, canStack);
				}
				else if (currentStates[i].MaxStacksCount == 0 || currentStates[i].CurrentStacksCount == currentStates[i].MaxStacksCount)
				{
					var canStack = currentStates[i].Stack(duration);
					_stateIcons.ActivateIco(state, duration, 0, canStack);
				}
				MoveStateToEnd(i);
				return;
			}
		}

		// Если состояние отсутствует, создаем новое
		AbstractCharacterState stateInstance = enumToState[state];
		Health characterHealth = _hero.Health;
		float chanceDodgeMagDamage = Random.Range(0f, 100f);

		if (!isCanDodgeMagState)
		{
			// Проверка на сопротивление магическому урону
			if (stateInstance.Type == StateType.Magic && chanceDodgeMagDamage <= characterHealth.ResistMagDamage)
			{
				Debug.Log("CharacterState / DodgeMagDamage");
				return;
			}
		}

		// Создаем новое состояние и добавляем в конец списка
		CreateState(stateInstance, state, duration, damageToExit, personWhoShooted, skillName, false);

		// Если состояние — щит, добавляем его в Health
		if (stateInstance is IDamageable damageableShield)
		{
			AddShield(damageableShield);
		}

		// Если нужно указать школу заклинаний, обновляем контрспелл
		if (school != Schools.None)
		{
			var counterSpell = (AbilitySchoolDebuff)stateInstance;
			counterSpell.canceledSchoool = school;
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
			//Debug.Log("Add Shield By " + shield);
			health.Shields.Add(shield);
		}
	}

	private void RemoveShield(IDamageable shield)
	{
		var health = _hero.GetComponent<Health>();
		if (health != null)
		{
			//Debug.Log("Remove Shield By " + shield);
			health.Shields.Remove(shield);
		}
	}

	public void DispelStates(StateType type, int targetTeamIndex, int playerTeamIndex, bool isDispelOneState = false)
	{

		if (currentStates.Count == 0) return;

		List<AbstractCharacterState> statesToRemove = new List<AbstractCharacterState>();

		for (int i = currentStates.Count - 1; i >= 0; i--)
		{
			AbstractCharacterState state = currentStates[i];

			if (state.Type == type &&
				((targetTeamIndex == playerTeamIndex && state.BaffDebaff == BaffDebaff.Debaff) ||
				 (targetTeamIndex != playerTeamIndex && state.BaffDebaff == BaffDebaff.Baff)))
			{
				if (state.CurrentStacksCount > 1)
				{
					state.CurrentStacksCount--;
					ClientRpcRemoveIconCount();
				}
				else
				{
					statesToRemove.Add(state);
					if (isDispelOneState) break;
				}

				break;
			}
		}

		foreach (var state in statesToRemove)
		{
			CmdRemoveState(state.State);
			//state.ExitState();
			//currentStates.Remove(state);
			//RemoveStateLogic(state.State);
		}

		//RemoveStateLogic(statesToRemove.Select(s => s.State).ToList());
	}


	[ClientRpc]
	private void ClientRpcRemoveIconCount()
	{
		_stateIcons?.RemoveIconCount();
	}

	private void MoveStateToEnd(int index)
	{
		if (index < 0 || index >= currentStates.Count)
			return;

		// Сохраняем ссылку на состояние
		var state = currentStates[index];

		// Удаляем элемент из текущей позиции
		currentStates.RemoveAt(index);

		// Добавляем его в конец списка
		currentStates.Add(state);
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
	BleedingCarrigan,
	ReducingHealing,
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
	Absorption,
	EmeraldSkin,
	SparkTalentHealthBuff,
	DefenseReduction,
	SelfHarm,
	ShieldBaff
}
public enum BaffDebaff
{
	Baff,
	Debaff,
}