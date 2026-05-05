using System;
using Gangdollarff.EarthElemental;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class StateInfo
{
    public States State;
    public float Duration;
    public float DamageToExit;
    public GameObject PersonWhoShooted;
    public string SkillName;

	public StateInfo(States state, float duration, float damageToExit, GameObject personWhoShooted, string skillName)
    {
        State = state;
        Duration = duration;
        DamageToExit = damageToExit;
        PersonWhoShooted = personWhoShooted;
        SkillName = skillName;
    }
}

public abstract class AbstractCharacterState
{
	protected CharacterState characterState;
	protected SkillManager abilities;
	protected Health health;
	protected Character personWhoMadeBuff;
	protected Skill skill;
	protected Schools _schoolState;

	protected int currentStacksCount = 0;
	protected bool isHidden = false;

	public int CurrentStacksCount => currentStacksCount;

	public Skill Skill => skill;
    public int MaxStacksCount = 0;
	protected float duration = -1;
	protected float damageToExit = 0;
	//protected float _duration;
	//public bool CanStack = true;

	public virtual float RemainingDuration
	{
		get => duration;
		set => duration = value;
	}
	public bool IsHidden => isHidden;
	public Character PersonWhoMadeBuff => personWhoMadeBuff;
	public abstract States State { get; }
	public abstract StateType Type { get; }
	public abstract BaffDebaff BaffDebaff { get; }
	public abstract List<StatusEffect> Effects { get; }
	public virtual Schools Schools { get; }
	public virtual DispelType dispelType => DispelType.None;

    public virtual AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		if(!CanEnterState(character)) return null;

		BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);  
		
		return this;
	}

	public abstract void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName);
    public abstract void UpdateState();

    public virtual void GloabalUpdate()
	{
		UpdateState();
		if(duration >= 0 && duration != -1)
		{
			duration -= Time.deltaTime;

			if(duration <= 0)
			{
				if(currentStacksCount > 0)
				{
					ReduceStack();
                }
				else
					ExitState();
			}
		}
    }

	public virtual void ExitState()
	{
        characterState.RemoveState(this);
    }
	
	public virtual bool Stack(float time)
	{
		duration = time;
		return true; 
	}

	public virtual void ReduceStack()
	{
        ExitState();
    }

	protected virtual bool CanEnterState(CharacterState character)
	{
		return true; 
	}

	protected virtual void BaseInit(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
        characterState = character;
        health = character.Character.Health;
        abilities = character.Character.Abilities;
        this.personWhoMadeBuff = personWhoMadeBuff;
        duration = durationToExit;

        if (this.damageToExit == 0)
        {
            this.damageToExit = 10000;
        }
        else
        {
            this.damageToExit = damageToExit;
        }
        this.personWhoMadeBuff = personWhoMadeBuff;

        skill = abilities.Abilities.FirstOrDefault(x => x.Name == skillName);
    }
}

public abstract class StackableState : AbstractCharacterState
{
	public override Schools Schools => Schools.Physical;

    public override bool Stack(float time)
	{
		duration = time;
		return true; 
	}
}

public abstract class RefreshingState : StackableState
{
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
			EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
		else
			Stack(duration);

		currentStacksCount++;

			return this;
    }

    public override void ReduceStack()
    {
		ExitState();
    }
}

public abstract class IndependentState: StackableState
{
    public override bool Stack(float time)
    {
		if(currentStacksCount >= MaxStacksCount)
		{
			//_timers
		}
		else
		{
			currentStacksCount++;
		}
		return false;
    }

    public override void ReduceStack()
    {
        currentStacksCount--;
    }
}

public abstract class AuraState : AbstractCharacterState
{
	protected Character _self;
    private Transform _auraCentre;
    protected List<Character> _charactersInRadius = new();
    private List<Collider> _collidersKeysForRemove = new();
	private Dictionary<Collider, Character> _colliderToCharacter = new();
	private float _timeAfterLastEffect = 0;

	public abstract float Distance { get; }
    public abstract float EffectRate { get; }
    public abstract LayerMask LayerMask { get; }

    public abstract void EffectOnEnter(Character character);
    public abstract void EffectOnExit(Character character);
    public abstract void EffectOnStay(List<Character> characters);

    public override StateType Type => StateType.Aura;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
		_auraCentre = character.transform;
		_self = personWhoMadeBuff;
		duration = durationToExit;
    }

    public override void UpdateState()
    {
        if (NetworkServer.active == false)
        {
            _timeAfterLastEffect += Time.deltaTime;

            if (EffectRate > _timeAfterLastEffect)
				return;

			_timeAfterLastEffect = 0;

            var colliders = Physics.OverlapSphere(_auraCentre.position, Distance, LayerMask);

            foreach (KeyValuePair<Collider, Character> collider in _colliderToCharacter)
			{
				if (colliders.Contains(collider.Key) == false)
				{
                    EffectOnExit(collider.Value);
					_charactersInRadius.Remove(collider.Value);
					_collidersKeysForRemove.Add(collider.Key);
				}
			}
			foreach (var item in _collidersKeysForRemove)
			{
				_colliderToCharacter.Remove(item);
			}
			_collidersKeysForRemove.Clear();

            foreach (var collider in colliders)
			{
				if (_colliderToCharacter.ContainsKey(collider) == false && collider.TryGetComponent(out Character character))
				{
					_colliderToCharacter.Add(collider, character);
					_charactersInRadius.Add(character);
					EffectOnEnter(character);
				}
			}

            EffectOnStay(_charactersInRadius);
        }
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);
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
	private List<AbstractCharacterState> _currentStates = new List<AbstractCharacterState>();
	[SerializeField] private StateIcons _stateIcons;
	[SerializeField] private StateEffects _stateEffects;

	public bool invinsible = false;

	public StateEffects StateEffects => _stateEffects;
	public StateIcons StateIcons => _stateIcons;
	public List<AbstractCharacterState> CurrentStates => _currentStates;
	public Character Character => _hero;
	public event System.Action<AbstractCharacterState> OnStateAdded;
	public event Action<States, int> OnStateDispelled;

	public Dictionary<States, AbstractCharacterState> enumToState = new Dictionary<States, AbstractCharacterState>()
	{
		#region UpdatedStates
		[States.Frozen] = new FrozenState(),
		[States.Frosting] = new FrostingState(),
		[States.Cooling] = new Cooling(),
		[States.Restoration] = new RestorationState(States.Restoration),
		[States.RestorationStacking] = new RestorationState(States.RestorationStacking),
		[States.Stun] = new StunnedState(),
		[States.Silent] = new Silent(),
		[States.Calmness] = new Calmness(),
		[States.PartialBlindness] = new PartialBlindness(),
		[States.ScorchedSoul] = new ScorchedSoul(),
		[States.Blind] = new BlindnessState(),
		[States.HealingSlime] = new HealingSlime(),
		#endregion


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
		[States.DisciplineAura]   = new DisciplineAuraState(),

		[States.Knockdown] = new Knockdown(),
		[States.IdealEvade] = new IdealEvade(),
		[States.BleedingDebuff] = new BleedingDebuff(),
		[States.EmeraldSkin] = new EmeraldSkinState(),
		[States.DefenseReduction] = new DefenceReductionState(),
		[States.SparkTalentHealthBuff] = new SparkTalentHealthState(),
		[States.SelfHarm] = new SelfHarmState(),
		[States.InAir] = new InAirState(),
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
		[States.BleedingScrader] = new BleedingScraderDebuff(),
		[States.ReducingHealing] = new ReducingHealingState(),
		[States.LowVoltage] = new LowVoltage(),
		[States.ComboState] = new ComboState(),
		[States.DisappointmentState] = new DisappointmentState(),
		[States.ManaRegen] = new ManaRegen(),
		[States.Stupefaction] = new Stupefaction(),
		[States.TentacleGrip] = new TentacleGrip(),
		[States.Destruction] = new DestructionState(States.Destruction),
		[States.DestructionStacking] = new DestructionState(States.DestructionStacking),
		[States.HardenedFlesh] = new HardenedFlesh(),
		[States.FocusingOnReflexesState] = new FocusingOnReflexesState(),
		[States.DivineEnhancement] = new DivineEnhancementState(),
		[States.DischargePsi] = new DischargePsiState(),
		[States.TrueSightState] = new TrueSight(),
		[States.CorrodedArmor] = new CorrodedArmorState(),
		[States.Impatience] = new ImpatienceState(),
		[States.PsionicGeneration] = new PsionicGenerationState(),
		[States.Parasites] = new ParasitesState(),
		[States.SwarmSpeed] = new SwarmSpeedState(),
		[States.DestructivePoison] = new DestructivePoisonState(),
		[States.InjectionAdrenaline] = new InjectionAdrenalineState(),
		[States.ProtectiveScales] = new ProtectiveScalesState(),
		[States.ErodedArmor] = new ErodedArmorState(),
		[States.ParalyzingPoison] = new ParalyzingPoisonState(),
		[States.FeelingPoisoning] = new FeelingPoisoningState(),
		[States.LightningEvade] = new LightningEvadeState(),
		[States.ReptilianStasis] = new ReptilianStasisState(),
		[States.ReflectiveScales] = new ReflectiveScalesState(),
		[States.SwiftAttacks] = new SwiftAttacksState(),
		[States.FireCharge] = new FireChargeState(),
		[States.RestorativeAttacks] = new RestorativeAttacksState(),
		[States.CounterRage] = new CounterRageState(),
		[States.Ignition] = new IgnitionState(),
		[States.MergeDark] = new MergeDarkState(),
		[States.DarkFormState] = new DarkFormState(),
		[States.ShackleState] = new ShackleState(),
		[States.SlowFlowLight] = new SlowFlowLightState(),
		[States.Retribution] = new RetributionState(),

		#region TerrifyingElfStates
		[States.InnerDarkness] = new InnerDarkness(),
		[States.Fear] = new Fear(),
		[States.Astral] = new AstralState(),
		[States.Irradiation] = new IrradiationState(),
		[States.Suppression] = new SuppressionState(),
		[States.WeakeningSilence] = new WeakeningSilence(),
		[States.Anxiety] = new Anxiety(),
		[States.HuntressMark] = new HuntressMark(),
		[States.Sleep] = new Sleep(),
		[States.ElvenSkill] = new ElvenSkill(),
		[States.Bound] = new Bound(),
		[States.ShadowTree] = new ShadowTree(),
		[States.MultiMagic] = new MultiMagic(),
		[States.FireFlash] = new FireFlash(),
		[States.WarmingUpState] = new WarmingUpState(),
		
		#endregion

		#region Gandollarf	
		[States.PowerOfEarth] = new PowerOfEarth(),
		[States.EarthsHealth] = new EarthsHealthBuff(),
		[States.MagicWater] = new MagicWater(),
		[States.HotBloodBuff] = new HotAuraBuff(),
        [States.GodAura] = new GodAura(),
        [States.GodAuraBuff] = new GodAuraBuff(),
        [States.TransformationDebuff] = new TransformationDebuff(),
        [States.PetrificationDebuff] = new PetrificationState(),
        [States.PushingWindBuff] = new PushingWindBuff(States.PushingWindBuff),
        [States.PushingWindAura] = new PushingWindBuff(States.PushingWindAura),
        [States.Burning] = new Burning(),
        [States.Burn] = new Burn(),
		[States.Discharge] = new Gangdollarff.AirElemental.Discharge(),
		[States.CoolingDamaged] = new CoolingDamaged(),
		[States.MagicalExcitement] = new MagicalExcitement(),
		[States.GodLight] = new GodLightState(),
		[States.MagicInstantaneity] = new MagicInstantaneityState(),
		[States.ImmortalityState] = new ImmortalityState(),
		#endregion

        #region Test Baff and Debaff
        [States.BaffState] = new BaffState(),
		[States.DebaffState] = new DebaffState(),
        #endregion

        #region Test
        [States.TestAuraState] = new TestAuraState(),
        #endregion
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
		if (_currentStates.Count > 0)
		{
			for (int i = 0; i < _currentStates.Count; i++)
			{
				_currentStates[i].GloabalUpdate();
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
				return true;
			}
		}
		return false;
	}

	public bool CheckForState(States state)
	{
		foreach (AbstractCharacterState states in _currentStates)
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
		foreach (AbstractCharacterState states in _currentStates)
		{
			if (states.State == state)
			{
				return states.CurrentStacksCount;
			}
		}
		return 0;
	}
	public bool CheckStateType(StateType type)
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

	public bool HasMagicDebuff()
	{
		foreach (var state in _currentStates) if (state.Type == StateType.Magic && state.BaffDebaff == BaffDebaff.Debaff) return true;
		return false;
	}

	public AbstractCharacterState GetState(States state)
	{
		foreach (AbstractCharacterState states in _currentStates)
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
	
	public void AddState(States state, float duration, float damageToExit, Schools schools, GameObject personWhoShooted, string skillName)
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
		if (!_currentStates.Contains(newState)) return;

		//newState.CurrentStacksCount = 0;

		if (newState is IDamageable damageableShield)
		{
			RemoveShield(damageableShield);
		}
        if (_currentStates.Contains(newState))
		{
            _currentStates.Remove(newState);
			_stateIcons?.RemoveItemByState(newState.State);
		}
    }

	private void RemoveStateLogic(States stateName)
	{
		if (_currentStates.Count <= 0) return;

		var statesCopy = new List<AbstractCharacterState>(_currentStates);

		foreach (var state in statesCopy)
		{
			if (state.State == stateName)
			{
				if (state is IDamageable damageableShield)
				{
					RemoveShield(damageableShield);
				}

				state.ExitState();
				_currentStates.Remove(state);

				_stateIcons?.RemoveItemByState(stateName);
				break;
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

	public void AddStateLogic(States state, float duration, float damageToExit, Schools school, GameObject personWhoShooted, string skillName, bool isCanDodgeMagState = false)
	{
		if (invinsible) return;

		for (int i = 0; i < _currentStates.Count; i++)
		{
			if (_currentStates[i].State == state)
			{
				if (personWhoShooted.TryGetComponent<Character>(out var character))
                {
                    _currentStates[_currentStates.Count - 1].TryApply(this, duration, damageToExit, character, skillName);
                }
                else
                {
                    _currentStates[_currentStates.Count - 1].TryApply(this, duration, damageToExit, null, skillName);
                }
                //_currentStates[i].TryApply(this, duration, damageToExit, character, skillName);

                if ((_currentStates[i] is RefreshingState) == false) break;
				if (_currentStates[i].MaxStacksCount == 0)
                {
					bool canStack = _currentStates[i].Stack(duration);
					int newMaxStack = _currentStates[i].MaxStacksCount;
                    if (!_currentStates[i].IsHidden)
                        _stateIcons.ActivateIco(state, duration, 1, false, newMaxStack);
					

					float timeForIcon = duration;
					if (state == States.Restoration || state == States.Destruction)
					{
						timeForIcon = _currentStates[i].RemainingDuration > 0f ? _currentStates[i].RemainingDuration : duration;
					}
					_stateIcons.ActivateIco(state, timeForIcon, 1, canStack, newMaxStack);

					MoveStateToEnd(i);
				}

				else
                {
					//_currentStates[i].Stack(duration);
					//_currentStates[i].duration = Mathf.Max(_currentStates[i].RemainingDuration, duration);
					float remaining = _currentStates[i].RemainingDuration > 0f ? _currentStates[i].RemainingDuration : duration;

					int newMaxStack = _currentStates[i].MaxStacksCount;
                    if (!_currentStates[i].IsHidden)
                        _stateIcons.ActivateIco(state, remaining, 1, true, newMaxStack);

					MoveStateToEnd(i);
				}
				
				return;
			}
		}

		AbstractCharacterState stateInstance = enumToState[state];
		Health characterHealth = _hero.Health;
		float chanceDodgeMagDamage = Random.Range(0f, 100f);

		if (!isCanDodgeMagState)
		{
			if (stateInstance.Type == StateType.Magic && chanceDodgeMagDamage <= characterHealth.ResistMagDamage)
			{
				Debug.Log("CharacterState / DodgeMagDamage");
				return;
			}
		}

		CreateState(stateInstance, state, duration, damageToExit, personWhoShooted, skillName, false);

		OnStateAdded?.Invoke(stateInstance);

		if (stateInstance is IDamageable damageableShield)
		{
			AddShield(damageableShield);
		}

		/*if (school != Schools.None)
		{
			var counterSpell = (AbilitySchoolDebuff)stateInstance;
			counterSpell.canceledSchoool = school;
		}*/
	}

	private void CreateState(AbstractCharacterState state, States stateName, float duration, float damageToExit, GameObject personWhoShooted, string skillName, bool stack)
	{
		_currentStates.Add(state);

		//state.duration = duration;

		if (personWhoShooted.TryGetComponent<Character>(out var character))
		{
			_currentStates[_currentStates.Count - 1].TryApply(this, duration, damageToExit, character, skillName);
		}
		else
		{
			_currentStates[_currentStates.Count - 1].TryApply(this, duration, damageToExit, null, skillName);
		}

		float remaining = state.RemainingDuration;
		int maxStacksCount = state.MaxStacksCount;
		if(!state.IsHidden)
			_stateIcons.ActivateIco(stateName, remaining, 1, stack, maxStacksCount);
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

	public void DispelStates(StateType type, int targetTeamIndex, int playerTeamIndex, bool isDispelOneState = false)
	{
		if (_currentStates.Count == 0) return;

		List<AbstractCharacterState> statesToRemove = new List<AbstractCharacterState>();

		for (int i = _currentStates.Count - 1; i >= 0; i--)
		{
			AbstractCharacterState state = _currentStates[i];

			if (state.Type == type &&
				((targetTeamIndex == playerTeamIndex && state.BaffDebaff == BaffDebaff.Debaff) ||
				 (targetTeamIndex != playerTeamIndex && state.BaffDebaff == BaffDebaff.Baff)))
			{
				if (state.CurrentStacksCount > 1)
				{
					//state.currentStacksCount--;
					ClientRpcRemoveIconCount();
				}
				else
				{
					statesToRemove.Add(state);
					if (isDispelOneState) break;
				}
			}
		}

		foreach (var state in statesToRemove)
		{
			RemoveState(state.State);
			_stateIcons.RemoveItemByState(state.State);
		}
	}

	public void DispelStates(StateType type, bool isAlly,out int howMuchDispelled , bool isDispelOneState = false)
	{
		howMuchDispelled = 0;
		if (_currentStates.Count == 0) return;

		List<AbstractCharacterState> statesToRemove = new List<AbstractCharacterState>();

		for (int i = _currentStates.Count - 1; i >= 0; i--)
		{
			AbstractCharacterState state = _currentStates[i];

			if (state.Type == type &&
				((isAlly && state.BaffDebaff == BaffDebaff.Baff) ||
				 (!isAlly && state.BaffDebaff == BaffDebaff.Debaff)))
			{
				if(state.PersonWhoMadeBuff != null)
					NotifyDispelWhoMade(state.PersonWhoMadeBuff.gameObject,state.State,state.CurrentStacksCount);
				if (state.CurrentStacksCount > 1)
				{
					//state.currentStacksCount--;
					ClientRpcRemoveIconCount();
				}
				else
				{
					statesToRemove.Add(state);
					if (isDispelOneState) break;
				}
			}
		}

		foreach (var state in statesToRemove)
		{
			RemoveState(state.State);
			_stateIcons.RemoveItemByState(state.State);
		}
	}
	
	[ClientRpc]
	private void NotifyDispelWhoMade(GameObject whoMade, States state,int num)
	{
		if(whoMade == null) return;
		whoMade.TryGetComponent(out Character c);
		if(c == null) return;
		
		c.CharacterState.OnOwnStateDispelled(state,num);
	}

	public void OnOwnStateDispelled(States state, int num)
	{
		if (!isOwned) return;
		OnStateDispelled?.Invoke(state,num);
	}

	[ClientRpc]
	private void ClientRpcRemoveIconCount()
	{
		_stateIcons?.RemoveIconCount();
	}

	[ClientRpc]
	private void RpcClearStateIcons()
	{
		_stateIcons?.DeactivateAll();
	}

	private void MoveStateToEnd(int index)
	{
		if (index < 0 || index >= _currentStates.Count)
			return;

		// Сохраняем ссылку на состояние
		var state = _currentStates[index];

		// Удаляем элемент из текущей позиции
		_currentStates.RemoveAt(index);

		// Добавляем его в конец списка
		_currentStates.Add(state);
	}

	[Server]
	public void ServerClearAllStates()
	{
		var statesCopy = new List<AbstractCharacterState>(_currentStates);

		foreach (var state in statesCopy) state.ExitState();
		_currentStates.Clear();
		RpcClearStateIcons();
	}
}

public enum StateType
{
	Physical,
	Magic,
	Immaterial,
	Aura
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
	Restoration,
	Destruction,
	Evade,
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
	ShieldBaff,
	LowVoltage,
	ComboState,
	DisappointmentState,
	ManaRegen,
	InnerDarkness,
	Fear,
	Astral,
	Silent,
	Irradiation,
	Suppression,
	WeakeningSilence,
	PartialBlindness,
	Anxiety,
	HuntressMark,
	Calmness,
	Sleep,
	ElvenSkill,
	BaffState,
    DebaffState,
	Bound,
	ShadowTree,
    PowerOfEarth,
    EarthsHealth,
    MagicWater,
    Burning,
    Burn,
    TestAuraState,
	MultiMagic,
	FireFlash,
	Stupefaction,
	TentacleGrip,
    Discharge,
    Restoration,
    RestorationStacking,
    Destruction,
    DestructionStacking,
	HardenedFlesh,
	FocusingOnReflexesState,
	WarmingUpState,
	DivineEnhancement,
	HealingSlime,
	BleedingScrader,
	DischargePsi,
	BleedingDebuff,
	TrueSightState,
	CorrodedArmor,
	Impatience,
	PsionicGeneration,
	MagicalExcitement,
	GodLight,
	HotBloodBuff,
	GodAura,
	GodAuraBuff,
	TransformationDebuff,
	PetrificationDebuff,
	PushingWindBuff,
	PushingWindAura,
	CoolingDamaged,
	MagicInstantaneity,
	ImmortalityState,
	Parasites,
	SwarmSpeed,
	DestructivePoison,
	InjectionAdrenaline,
	ProtectiveScales,
	ErodedArmor,
	ParalyzingPoison,
	FeelingPoisoning,
	LightningEvade,
	ReptilianStasis,
	ReflectiveScales,
	SwiftAttacks,
	CounterRage,
	Ignition,
	MergeDark,
	DarkFormState,
	ShackleState,
	SlowFlowLight,
	Retribution,
	DisciplineAura,
	FireCharge,
	RestorativeAttacks
}
public enum BaffDebaff
{
	Baff,
	Debaff,
	Null,
}

public enum DispelType
{
	None,
	Magic,
	Physic,
    Immaterial
}