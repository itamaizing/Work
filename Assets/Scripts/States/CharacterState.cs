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

public interface ITickableState
{
	float TickInterval { get; }
	void Tick();
}

public enum StateParameter
{
	DamageToExit,
	IsCanDodgeMagState,
	ExtraDuration,
}

public abstract class AbstractCharacterState
{
    protected CharacterState characterState;
    protected SkillManager abilities;
    protected Health health;
    protected Character sourceCaster;
    protected Skill skill;

    protected bool isHidden = false;
    public bool IsHidden => isHidden;

    public abstract States State { get; }
    public abstract StateType Type { get; }
    public abstract BaffDebaff BaffDebaff { get; }
    public abstract List<StatusEffect> Effects { get; }
    public virtual Schools Schools { get; }
    public virtual DispelType dispelType => DispelType.None;
    public virtual DiminishingReturnGroup DrGroup => DiminishingReturnGroup.None;

    public Character SourceCaster => sourceCaster;
    public Character Target => characterState?.Character;
    public Skill Skill => skill;

    public virtual bool IsUnique => true;
    public virtual bool CanExceedMaxDuration => false;
    public virtual AbstractCharacterState Clone() => null;
    public virtual int CurrentStacksCount => 0;
    public virtual int MaxStacksCount => 1;
    public double StartTime { get; private set; }
    
    public virtual string TooltipName => State.ToString();
    public virtual string TooltipDescription => string.Empty;

    protected readonly Dictionary<StateParameter, object> parameters = new();

    protected float DamageToExit =>
        parameters.TryGetValue(StateParameter.DamageToExit, out var v) ? (float)v : float.MaxValue;

    private float _duration = -1f;
    private float _maxDuration = -1f;
    private string _displayText = "";
    private float _tickTimer;
    private bool _hasApplied;

    public event System.Action<AbstractCharacterState, float, float> OnDurationChanged;
    public event System.Action<AbstractCharacterState, string> OnTextChanged;

    public virtual float RemainingDuration
    {
        get => _duration;
        protected set { _duration = value; OnDurationChanged?.Invoke(this, _duration, MaxDuration); }
    }

    public virtual float MaxDuration
    {
        get => _maxDuration;
        protected set => _maxDuration = value;
    }
    
    public void SetDuration(float value)
    {
	    RemainingDuration = CanExceedMaxDuration ? value : Mathf.Min(value, MaxDuration);
    }
    
    public void IncreaseDuration(float delta) => SetDuration(RemainingDuration + delta);

    public string DisplayText
    {
        get => _displayText;
        protected set { _displayText = value; OnTextChanged?.Invoke(this, _displayText); }
    }

    public virtual bool TryApply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName)
    {
        if (!CanEnterState(character)) return false;

        BaseInit(character, durationToExit, damageToExit, sourceCaster, skillName);

        if (!_hasApplied)
        {
            _hasApplied = true;
            Apply(character, durationToExit, damageToExit, sourceCaster, skillName);
        }
        else
        {
            Reapply(character, durationToExit, damageToExit, sourceCaster, skillName);
        }

        return true;
    }
    
    public abstract void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName);
    
    public virtual void Reapply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName) =>
        Apply(character, durationToExit, damageToExit, sourceCaster, skillName);

    public abstract void UpdateState();
    
    protected void ProcessTick()
    {
	    if (this is not ITickableState tickable) return;

	    _tickTimer -= Time.deltaTime;
	    if (_tickTimer <= 0f)
	    {
		    tickable.Tick();
		    _tickTimer = tickable.TickInterval;
	    }
    }

    public virtual void GlobalUpdate()
    {
	    UpdateState();
	    ProcessTick();

	    if (RemainingDuration >= 0f)
	    {
		    RemainingDuration -= Time.deltaTime;
		    if (RemainingDuration <= 0f) ExitState();
	    }
    }

    public virtual void ExitState() => characterState.RemoveState(this);

    protected virtual bool CanEnterState(CharacterState character) => true;

    protected virtual void BaseInit(CharacterState character, float durationToExit, float damageToExit,
	    Character sourceCaster, string skillName)
    {
	    characterState = character;
	    health = character.Character.Health;
	    abilities = character.Character.Abilities;
	    this.sourceCaster = sourceCaster;

	    if (StartTime == 0) StartTime = NetworkTime.time;

	    RemainingDuration = durationToExit;
	    if (MaxDuration <= 0f) MaxDuration = durationToExit;

	    if (damageToExit > 0f) parameters[StateParameter.DamageToExit] = damageToExit;
	    if (this is ITickableState tickable && _tickTimer <= 0f) _tickTimer = tickable.TickInterval;
    }
    
    public virtual bool TryDispel(DispelType incomingDispel)
    {
	    if (dispelType == DispelType.None) return false;
	    if ((dispelType & incomingDispel) == 0) return false;

	    ExitState();
	    return true;
    }
}

public abstract class StateStacking : AbstractCharacterState
{
	protected int currentStacksCount = 0;
	public override int CurrentStacksCount => currentStacksCount;

	private int _maxStacksCount = 1;
	public override int MaxStacksCount => _maxStacksCount;
	public void SetMaxStacks(int value) => _maxStacksCount = value;
	
	public override bool TryApply(CharacterState character, float durationToExit, float damageToExit,
		Character sourceCaster, string skillName)
	{
		bool wasFirstApply = currentStacksCount == 0;
		bool applied = base.TryApply(character, durationToExit, damageToExit, sourceCaster, skillName);

		if (applied && wasFirstApply)
		{
			currentStacksCount = 1;
			UpdateDisplayText();
		}

		return applied;
	}

	public override void Reapply(CharacterState character, float durationToExit, float damageToExit,
		Character sourceCaster, string skillName) => Stack(durationToExit);

	public virtual bool Stack(float time)
	{
		RemainingDuration = MaxDuration;
		return true;
	}

	public virtual void ReduceStack()
	{
		if (currentStacksCount <= 1)
		{
			ExitState();
			return;
		}

		currentStacksCount--;
		UpdateDisplayText();
	}

	protected virtual void UpdateDisplayText() => DisplayText = currentStacksCount.ToString();

	public override void GlobalUpdate()
	{
		UpdateState();
		ProcessTick();

		RemainingDuration -= Time.deltaTime;

		if (RemainingDuration <= 0f)
		{
			ReduceStack();
		}
	}
}

public abstract class RefreshingStateStacking : StateStacking
{
	public virtual float StackLingerTime => 0.2f;

	public override void ReduceStack()
	{
		if (currentStacksCount <= 1)
		{
			ExitState();
			return;
		}

		currentStacksCount--;
		UpdateDisplayText();
		RemainingDuration = StackLingerTime;
	}
}

public class StackInfo
{
	public float MaxDuration;
	public float CurrentDuration;

	public StackInfo(float duration)
	{
		MaxDuration = duration;
		CurrentDuration = duration;
	}
}

public abstract class StackingIndependentState : StateStacking
{
	protected readonly List<StackInfo> stacks = new();

	public override bool Stack(float time)
	{
		if (currentStacksCount < MaxStacksCount)
		{
			currentStacksCount++;
			UpdateDisplayText();
		}
		else
		{
			stacks.RemoveAt(0);
		}

		stacks.Add(CreateStackInfo(time));
		MaxDuration = stacks[^1].MaxDuration;
		RemainingDuration = stacks[^1].CurrentDuration;
		return true;
	}

	protected virtual StackInfo CreateStackInfo(float duration) => new StackInfo(duration);

	public override void ReduceStack()
	{
		if (currentStacksCount <= 1) { ExitState(); return; }
		currentStacksCount--;
		UpdateDisplayText();
		stacks.RemoveAt(0);
	}

	public override void GlobalUpdate()
	{
		UpdateState();
		ProcessTick();

		for (int i = stacks.Count - 1; i >= 0; i--)
		{
			stacks[i].CurrentDuration -= Time.deltaTime;
			if (stacks[i].CurrentDuration <= 0f) RemoveStackAt(i);
		}
	}

	protected void RemoveStackAt(int index)
	{
		stacks.RemoveAt(index);
		currentStacksCount--;
		UpdateDisplayText();

		if (stacks.Count == 0) ExitState();
		else { MaxDuration = stacks[^1].MaxDuration; RemainingDuration = stacks[^1].CurrentDuration; }
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

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
	    Character sourceCaster, string skillName)
    {
	    _auraCentre = character.transform;
	    _self = sourceCaster;
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

    /*public override bool Stack(float time)
    {
        return false;
    }*/
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

    [SyncVar] private bool _suppressStateEffectsBuff;
    public bool SuppressStateEffectsBuff => _suppressStateEffectsBuff;

    [Server]
    public void SetSuppressStateBuffEffects(bool value) => _suppressStateEffectsBuff = value;

    [SyncVar] private bool _suppressStateDebuffEffects;
    public bool SuppressStateEffects => _suppressStateDebuffEffects;

    [Server]
    public void SetSuppressStateDebuffEffects(bool value) => _suppressStateDebuffEffects = value;

    public StateEffects StateEffects => _stateEffects;
    public StateIcons StateIcons => _stateIcons;
    public List<AbstractCharacterState> CurrentStates => _currentStates;
    public Character Character => _hero;
    public event System.Action<AbstractCharacterState> OnStateAdded;
    public event Action<States, int> OnStateDispelled;
    public event Action<AbstractCharacterState> OnStateRemoved;


	public Dictionary<States, AbstractCharacterState> enumToState = new Dictionary<States, AbstractCharacterState>()
	{
		#region UpdatedStates
		[States.Frozen] = new FrozenStateStacking(),
		[States.Frosting] = new FrostingStateStacking(),
		[States.Cooling] = new Cooling(),
		[States.Restoration] = new RestorationStateStacking(States.Restoration),
		[States.RestorationStacking] = new RestorationStateStacking(States.RestorationStacking),
		[States.Stun] = new StunnedStateStacking(),
		[States.Silent] = new Silent(),
		[States.Calmness] = new Calmness(),
		[States.PartialBlindness] = new PartialBlindness(),
		[States.ScorchedSoul] = new ScorchedSoul(),
		[States.Blind] = new BlindnessStateStacking(),
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
		[States.SpiritEnergy] = new SpiritEnergyStateStacking(),
		[States.SpiritHealth] = new SpiritHealthStateStacking(),
		[States.DisciplineAura]   = new DisciplineAuraStateStacking(),

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
		[States.PoisonBone] = new PoisonBoneStateStacking(),
		[States.WitheringPoison] = new WitheringPoisonStateStacking(),
		[States.BindingPoison] = new BindingPoisonState(),
		[States.PoisonCloud] = new PoisonCloudStateStacking(),
		[States.HealingPoisonCloud] = new HealingPoisonCloudState(),
		[States.EmpathicPoisons] = new EmpathicPoisonsState(),
		[States.HealingPoisonPerSecond] = new HealingPoisonPerSecondState(),
		[States.InstantHealingPoison] = new InstantHealingPoisonState(),
		[States.RegeneratingPoison] = new RegeneratingPoisonState(),
		[States.HeatedGlands] = new HeatedGlandsState(),
		[States.AbsorptionOfPoison] = new AbsorptionOfPoisonsState(),
		[States.Bleeding] = new BleedingStateStacking(),
		[States.BleedingCarry] = new BleedingStateStackingCarry(),
		[States.BleedingScrader] = new BleedingScraderDebuff(),
		[States.ReducingHealing] = new ReducingHealingState(),
		[States.LowVoltage] = new LowVoltage(),
		[States.ComboState] = new ComboStateStacking(),
		[States.DisappointmentState] = new DisappointmentStateStacking(),
		[States.ManaRegen] = new ManaRegen(),
		[States.Stupefaction] = new Stupefaction(),
		[States.TentacleGrip] = new TentacleGrip(),
		[States.Destruction] = new DestructionStateStacking(States.Destruction),
		[States.DestructionStacking] = new DestructionStateStacking(States.DestructionStacking),
		[States.HardenedFlesh] = new HardenedFlesh(),
		[States.FocusingOnReflexesState] = new FocusingOnReflexesStateStacking(),
		[States.DivineEnhancement] = new DivineEnhancementState(),
		[States.DischargePsi] = new DischargePsiState(),
		[States.TrueSightState] = new TrueSight(),
		[States.CorrodedArmor] = new CorrodedArmorState(),
		[States.Impatience] = new ImpatienceStateStacking(),
		[States.PsionicGeneration] = new PsionicGenerationState(),
		[States.Parasites] = new ParasitesStateStacking(),
		[States.SwarmSpeed] = new SwarmSpeedStateStacking(),
		[States.DestructivePoison] = new DestructivePoisonStateStacking(),
		[States.InjectionAdrenaline] = new InjectionAdrenalineState(),
		[States.ProtectiveScales] = new ProtectiveScalesStateStacking(),
		[States.ErodedArmor] = new ErodedArmorStateStacking(),
		[States.ParalyzingPoison] = new ParalyzingPoisonState(),
		[States.FeelingPoisoning] = new FeelingPoisoningStateStacking(),
		[States.LightningEvade] = new LightningEvadeStateStacking(),
		[States.ReptilianStasis] = new ReptilianStasisStateStacking(),
		[States.ReflectiveScales] = new ReflectiveScalesStateStacking(),
		[States.SwiftAttacks] = new SwiftAttacksStateStacking(),
		[States.FireCharge] = new FireChargeState(),
		[States.RestorativeAttacks] = new RestorativeAttacksState(),
		[States.CounterRage] = new CounterRageStateStacking(),
		[States.Ignition] = new IgnitionStateStacking(),
		[States.MergeDark] = new MergeDarkState(),
		[States.DarkFormState] = new DarkFormState(),
		[States.ShackleState] = new ShackleState(),
		[States.SlowFlowLight] = new SlowFlowLightStateStacking(),
		[States.Retribution] = new RetributionStateStacking(),
		[States.KillingSpree] = new KillingSpreeStateStacking(),
		[States.FrostEnergy] = new FrostEnergyStateStacking(),
		[States.PortalDarkness] = new PortalDarknessStateStacking(),
		[States.CreeperCombo] = new CreeperComboStateStacking(),
		[States.OtherForces] = new OtherForceStateStacking(),
		[States.MagicShield] = new MagicShieldState(),
		[States.VampirismBuff] = new VampirismBuffStateStacking(),

		#region TerrifyingElfStates
		[States.InnerDarkness] = new InnerDarkness(),
		[States.Fear] = new Fear(),
		[States.Astral] = new AstralStateStacking(),
		[States.Irradiation] = new IrradiationState(),
		[States.Suppression] = new SuppressionState(),
		[States.WeakeningSilence] = new WeakeningSilence(),
		[States.Anxiety] = new Anxiety(),
		[States.HuntressMark] = new HuntressMark(),
		[States.Sleep] = new Sleep(),
		[States.ElvenSkill] = new ElvenSkill(),
		[States.ElvenReflexes] = new ElvenReflexesState(),
		[States.Bound] = new Bound(),
		[States.ShadowTree] = new ShadowTree(),
		[States.MultiMagic] = new MultiMagic(),
		[States.FireFlash] = new FireFlash(),
		[States.WarmingUpState] = new WarmingUpStateStacking(),
		
		#endregion

		#region Gandollarf	
		[States.PowerOfEarth] = new PowerOfEarth(),
		[States.EarthsHealth] = new EarthsHealthBuff(),
		[States.MagicWater] = new MagicWater(),
		[States.HotBloodBuff] = new HotAuraBuff(),
		[States.GodAuraBuff] = new GodAuraBuff(),
        [States.TransformationDebuff] = new TransformationDebuff(),
        [States.PetrificationDebuff] = new PetrificationStateStacking(),
        [States.PushingWindBuff] = new PushingWindBuff(States.PushingWindBuff),
        [States.PushingWindAura] = new PushingWindBuff(States.PushingWindAura),
        [States.Burning] = new Burning(),
        [States.BurningMatter] = new BurningMatterDebuff(),
        [States.Burn] = new Burn(),
		[States.Discharge] = new Gangdollarff.AirElemental.Discharge(),
		[States.CoolingDamaged] = new CoolingDamaged(),
		[States.MagicalExcitement] = new MagicalExcitement(),
		[States.GodLight] = new GodLightState(),
		[States.MagicInstantaneity] = new MagicInstantaneityStateStacking(),
		[States.ImmortalityState] = new ImmortalityState(),
		#endregion

        #region Test Baff and Debaff
        [States.BaffState] = new BaffState(),
		[States.DebaffState] = new DebaffState(),
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
				_currentStates[i].GlobalUpdate();
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
		foreach (AbstractCharacterState s in _currentStates)
		{
			if (s.State == state) return true;
		}
		return false;
	}

	public int CheckStateStacks(States state)
	{
		foreach (AbstractCharacterState s in _currentStates)
		{
			if (s.State == state) return s.CurrentStacksCount;
		}
		return 0;
	}

	public bool CheckStateType(StateType type)
	{
		foreach (AbstractCharacterState state in _currentStates)
		{
			if (state.Type == type) return true;
		}
		return false;
	}

	public bool HasMagicDebuff()
	{
		foreach (var state in _currentStates)
			if (state.Type == StateType.Magic && state.BaffDebaff == BaffDebaff.Debaff) return true;
		return false;
	}

    public AbstractCharacterState GetState(States state)
    {
        foreach (AbstractCharacterState s in _currentStates)
        {
            if (s.State == state) return s;
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

    [Command(requiresAuthority = false)]
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

        if (newState is IDamageable damageableShield) RemoveShield(damageableShield);

        _currentStates.Remove(newState);
        _stateIcons?.UnregisterState(newState);
        OnStateRemoved?.Invoke(newState);
    }

    private void RemoveStateLogic(States stateName)
    {
        if (_currentStates.Count <= 0) return;

        var statesCopy = new List<AbstractCharacterState>(_currentStates);

        foreach (var state in statesCopy)
        {
            if (state.State != stateName) continue;

            if (state is IDamageable damageableShield) RemoveShield(damageableShield);

            state.ExitState();
            _currentStates.Remove(state);
            _stateIcons?.UnregisterState(state);
            break;
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

    public void AddStateLogic(States state, float duration, float damageToExit, Schools school,
        GameObject personWhoShooted, string skillName, bool isCanDodgeMagState = false)
    {
        if (invinsible) return;

        AbstractCharacterState template = enumToState[state];

        if (_suppressStateEffectsBuff && template.BaffDebaff == BaffDebaff.Baff) return;
        if (_suppressStateDebuffEffects && template.BaffDebaff == BaffDebaff.Debaff) return;

        duration = ApplyDiminishingReturns(state, duration, personWhoShooted);
        if (duration <= 0f && duration != -1) return;

        personWhoShooted.TryGetComponent<Character>(out var sourceCaster);
        
        if (!template.IsUnique)
        {
            var clone = template.Clone();
            if (clone == null)
            {
                Debug.LogError($"State {state} has IsUnique=false but Clone() returns null");
                return;
            }
            CreateAndAddState(clone, sourceCaster, duration, damageToExit, skillName, isCanDodgeMagState, checkDodge: true, template.Type);
            return;
        }

        for (int i = 0; i < _currentStates.Count; i++)
        {
            if (_currentStates[i].State != state) continue;

            _currentStates[i].TryApply(this, duration, damageToExit, sourceCaster, skillName);
            MoveStateToEnd(i);
            return;
        }

        CreateAndAddState(template, sourceCaster, duration, damageToExit, skillName, isCanDodgeMagState, checkDodge: true, template.Type);
    }

    private void CreateAndAddState(AbstractCharacterState stateInstance, Character sourceCaster, float duration,
        float damageToExit, string skillName, bool isCanDodgeMagState, bool checkDodge, StateType type)
    {
        if (checkDodge && !isCanDodgeMagState && type == StateType.Magic)
        {
            float chanceDodgeMagDamage = Random.Range(0f, 100f);
            if (chanceDodgeMagDamage <= _hero.Health.ResistMagDamage)
            {
                Debug.Log("CharacterState / DodgeMagDamage");
                return;
            }
        }

        bool applied = stateInstance.TryApply(this, duration, damageToExit, sourceCaster, skillName);
        if (!applied) return;

        _currentStates.Add(stateInstance);
        OnStateAdded?.Invoke(stateInstance);

        if (stateInstance is IDamageable damageableShield) AddShield(damageableShield);

        if (!stateInstance.IsHidden) _stateIcons.RegisterState(stateInstance);
    }

    private void AddShield(IDamageable shield)
    {
        var health = _hero.GetComponent<Health>();
        if (health != null) health.Shields.Add(shield);
    }

    private void RemoveShield(IDamageable shield)
    {
        var health = _hero.GetComponent<Health>();
        if (health != null) health.Shields.Remove(shield);
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
                    ReduceStackAndSync(state);
                }
                else
                {
                    statesToRemove.Add(state);
                    if (isDispelOneState) break;
                }
            }
        }

        foreach (var state in statesToRemove) RemoveState(state.State);
    }

    public void DispelStates(StateType type, bool isAlly, out int howMuchDispelled, bool isDispelOneState = false)
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
                if (state.SourceCaster != null)
                    NotifyDispelWhoMade(state.SourceCaster.gameObject, state.State, state.CurrentStacksCount);

                if (state.CurrentStacksCount > 1)
                {
                    ReduceStackAndSync(state);
                }
                else
                {
                    statesToRemove.Add(state);
                    if (isDispelOneState) break;
                }
            }
        }

        foreach (var state in statesToRemove) RemoveState(state.State);
    }

    public void DispelStates(StateType type, int targetTeamIndex, int playerTeamIndex, int maxStatesToDispel)
    {
        if (_currentStates.Count == 0 || maxStatesToDispel <= 0) return;

        List<AbstractCharacterState> statesToRemove = new List<AbstractCharacterState>();

        for (int i = _currentStates.Count - 1; i >= 0 && statesToRemove.Count < maxStatesToDispel; i--)
        {
            AbstractCharacterState state = _currentStates[i];

            if (state.Type == type &&
                ((targetTeamIndex == playerTeamIndex && state.BaffDebaff == BaffDebaff.Debaff) ||
                 (targetTeamIndex != playerTeamIndex && state.BaffDebaff == BaffDebaff.Baff)))
            {
                if (state.CurrentStacksCount > 1) ReduceStackAndSync(state);
                else statesToRemove.Add(state);
            }
        }

        foreach (var state in statesToRemove) RemoveState(state.State);
    }

    public void DispelStatesStack(StateType type, BaffDebaff buffDebaff, int howMuchToDispel, out int dispelled)
    {
        dispelled = 0;
        if (_currentStates.Count == 0) return;

        AbstractCharacterState stateToDispel = _currentStates.LastOrDefault(c => c.Type == type && buffDebaff == c.BaffDebaff);
        if (stateToDispel == null) return;

        if (stateToDispel.SourceCaster != null)
            NotifyDispelWhoMade(stateToDispel.SourceCaster.gameObject, stateToDispel.State, stateToDispel.CurrentStacksCount);

        int available = stateToDispel.CurrentStacksCount;
        int toRemove = Mathf.Min(available, howMuchToDispel);

        if (toRemove >= available)
        {
            RemoveState(stateToDispel.State);
        }
        else
        {
            for (int i = 0; i < toRemove; i++) ReduceStackAndSync(stateToDispel);
        }

        dispelled = toRemove;
    }
    
    private void ReduceStackAndSync(AbstractCharacterState state)
    {
        if (state is StateStacking stacking)
        {
            stacking.ReduceStack();
            RpcReduceStack(state.State);
        }
    }

    [ClientRpc]
    private void RpcReduceStack(States state)
    {
        if (GetState(state) is StateStacking stacking) stacking.ReduceStack();
    }

    private float ApplyDiminishingReturns(States state, float duration, GameObject whoMadeBuff = null)
    {
        if (!enumToState.TryGetValue(state, out var stateInstance)) return duration;
        var group = stateInstance.DrGroup;
        if (group == DiminishingReturnGroup.None) return duration;

        DiminishingReturnsTracker tracker;
        if (whoMadeBuff == null)
            tracker = _hero?.GetComponent<DiminishingReturnsTracker>();
        else
            tracker = whoMadeBuff.GetComponent<DiminishingReturnsTracker>();
        if (tracker == null) return duration;

        float modified = tracker.GetModifiedDuration(group, duration);
        if (modified > 0f) tracker.ConsumeApplication(group);
        return modified;
    }

    [ClientRpc]
    private void NotifyDispelWhoMade(GameObject whoMade, States state, int num)
    {
        if (whoMade == null) return;
        whoMade.TryGetComponent(out Character c);
        if (c == null) return;

        c.CharacterState.OnOwnStateDispelled(state, num);
    }

    public void OnOwnStateDispelled(States state, int num)
    {
        if (!isOwned) return;
        OnStateDispelled?.Invoke(state, num);
    }

    [ClientRpc]
    private void RpcClearStateIcons()
    {
        _stateIcons?.DeactivateAll();
    }

    private void MoveStateToEnd(int index)
    {
        if (index < 0 || index >= _currentStates.Count) return;

        var state = _currentStates[index];
        _currentStates.RemoveAt(index);
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
	Strengthening,
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
	BleedingCarry,
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
    BurningMatter,
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
	FrostEnergy,
	PortalDarkness,
	CreeperCombo,
	ElvenReflexes,
	FireCharge,
	RestorativeAttacks,
	KillingSpree,
	OtherForces,
	MagicShield,
	VampirismBuff
}
public enum BaffDebaff
{
	Baff,
	Debaff,
	Null,
}

[System.Flags]
public enum DispelType
{
	None = 0,
	Magic = 1,
	Physic = 2,
	Immaterial = 4,
}