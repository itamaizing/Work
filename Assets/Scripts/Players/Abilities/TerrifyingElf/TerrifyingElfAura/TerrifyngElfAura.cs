using Mirror;
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class TerrifyingElfAura : Skill
{
    [Header("Chances")] [SerializeField, Range(0f, 100f)]
    private float calmnessChance = 10f;

    [SerializeField, Range(0, 100)] private float elvenSkillFromPhysChance = 10f;
    [SerializeField, Range(0, 100)] private float calmnessOnElvenSkillChance = 30f;
    [SerializeField, Range(0f, 100f)] private float huntressMarkApplyChance = 5f;
    private const float innerDarknessChance = 20f;

    [Header("Durations")] 
    [SerializeField] private float durationCalmess;
    [SerializeField] private float durationHuntressMark;
    [SerializeField] private float durationElvenSkill;

    [Header("Effects")] [SerializeField] private GameObject elvenSkillEffect;

    [Header("Radius")] [SerializeField] private float radiusTreeCalmess = 12f;

    [Header("Skills")] [SerializeField] private ReconnaissanceFire reconnaissanceFire;

    #region Calmness aura

    [SerializeField] private float _auraTick = 1f;
    [SerializeField] private LayerMask _characterLayer;

    #endregion

    #region Physical skill crit

    [SerializeField] private Shot _shot;
    [SerializeField] private ShotIntoSky _shotIntoSky;
    [SerializeField] private ShotDarkness _shotDarkness;

    #endregion

    public GameObject ElvenSkillEffect
    {
        get => elvenSkillEffect;
        set => elvenSkillEffect = value;
    }

    private SkillManager _skillManager;

    #region boolTalent

    private bool calmnessTalent;
    private bool fireWorshipperTalent;
    private bool treeRadiusCalmessTalent;
    private bool huntressMarkPhysicsTalent;
    private bool manaAbsorptionPhysicalTalent;
    private bool elvenSkillTalent;
    private bool elvenSkillPhysicsTalent = false;
    private bool calmnessOnElvenSkillTalent;
    private bool suppressionManaAbsorptionTalent;
    private bool _isReductionRecharge;
    private bool _isElvenSkillPhysDamageHealthChance;
    private bool _isThirdShotRow;
    private bool _isCalmnessAura;
    private bool _isSpellAddInnerDarkness;
    
    public bool IsThirdShotRowActive => _isThirdShotRow;

    public void SpellAddInnerDarkness(bool value) => _isSpellAddInnerDarkness = value;

    public void ThirdShotRow(bool value) => _isThirdShotRow = value;
    public void ReductionRecharge(bool value) => _isReductionRecharge = value;

    public void CalmnessTalentActive(bool value)
    {
        if (value == calmnessTalent) return;
        calmnessTalent = value;
    }

    public void ElvenSkillPhysDamageHealthChance(bool value) => _isElvenSkillPhysDamageHealthChance = value;

    #region CalmlessOnAllyTalent
    
    private bool _isCalmnessAllyTalent;
    private readonly HashSet<Character> _trackedAllies = new();
    private int _totalAllyCalmnessStacks = 0;
    private const float ManaPerCalmnessStack = 0.03f;
    private Coroutine _allyCalmnessTracker;
    public void CalmnessAura(bool value)
    {
        _isCalmnessAllyTalent = value;

        if (!value)
        {
            if (_allyCalmnessTracker != null)
            {
                StopCoroutine(_allyCalmnessTracker);
                _allyCalmnessTracker = null;
            }
            _trackedAllies.Clear();
            _totalAllyCalmnessStacks = 0;
            if(isClient)
                CmdUpdateManaMaxModifier(_totalAllyCalmnessStacks);
        }
    }

    private void ApplyCalmnessTalent()
    {
        if (!calmnessTalent || currentSkill == null) return;

        if (currentSkill.Info.AbilityForm == AbilityForm.Magic)
        {
            var character = currentSkill.Hero;
            if (character != null && character.CharacterState != null)
            {
                bool isCalmnessChance = UnityEngine.Random.Range(0f, 100f) <= calmnessChance;

                if (isCalmnessChance)
                {
                    character.CharacterState.CmdAddState(States.Calmness, durationCalmess, 0f,
                        this.gameObject, currentSkill.Name);

                    if (treeRadiusCalmessTalent)
                    {
                        int treesCount = GetTreesCountInRadius(radiusTreeCalmess);
                        StartCoroutine(DelayAndUpdateCalmness(character.CharacterState, treesCount));
                    }

                    if (_isCalmnessAllyTalent)
                        ApplyCalmnessToAllies(durationCalmess);
                }
            }
        }

        currentSkill = null;
    }

    private void ApplyCalmnessToAllies(float duration)
    {
        if (_hero == null) return;

        float visionRange = _hero.VisionComponent.VisionRange;
        var colliders = Physics.OverlapSphere(_hero.transform.position, visionRange, _characterLayer);

        foreach (var col in colliders)
        {
            if (!col.TryGetComponent<Character>(out var target)) continue;
            if (target == _hero) continue;
            if (target.CharacterState == null) continue;

            target.CharacterState.AddState(States.Calmness, duration, 0f, _hero.gameObject, "CalmnessAura");
            _trackedAllies.Add(target);
        }

        if (_trackedAllies.Count > 0 && _allyCalmnessTracker == null)
            _allyCalmnessTracker = StartCoroutine(TrackAllyCalmnessStacks());
    }

    [Command]
    private void CmdAddCalmness(GameObject target, float duration)
    {
        target.GetComponent<CharacterState>().AddState(States.Calmness, duration, 0f, _hero.gameObject, "CalmnessAura");
    }
    
    private IEnumerator TrackAllyCalmnessStacks()
    {
        var wait = new WaitForSeconds(0.5f);

        while (_isCalmnessAllyTalent && _trackedAllies.Count > 0)
        {
            RecalcTotalAllyStacks();
            yield return wait;
        }

        if (_totalAllyCalmnessStacks != 0)
        {
            _totalAllyCalmnessStacks = 0;
            CmdUpdateManaMaxModifier(_totalAllyCalmnessStacks);
        }
        _allyCalmnessTracker = null;
    }

    private void RecalcTotalAllyStacks()
    {
        int total = 0;
        var toRemove = new List<Character>();

        foreach (var ally in _trackedAllies)
        {
            if (ally == null)
            {
                toRemove.Add(ally);
                continue;
            }

            if (!ally.CharacterState.CheckForState(States.Calmness))
            {
                toRemove.Add(ally);
                continue;
            }

            if (ally.CharacterState.GetState(States.Calmness) is Calmness state) 
            {
                total += state.CurrentStacksCount;
            }
        }
        foreach (var key in toRemove)
        {
            _trackedAllies.Remove(key);
        }

        if (total != _totalAllyCalmnessStacks) 
        {
            _totalAllyCalmnessStacks = total;
            CmdUpdateManaMaxModifier(_totalAllyCalmnessStacks);
        }
    }

    [Command]
    private void CmdUpdateManaMaxModifier(int totalAllyCount)
    {
        if (Hero?.TryGetResource(ResourceType.Mana) is not Resource mana) return;
        
        mana.RemoveModifierBySource(ResourceAttributeName.MaxValue, this, all: true);

        if (totalAllyCount > 0)
        {
            float bonus = ManaPerCalmnessStack * totalAllyCount;
            mana.AddModifier(ResourceAttributeName.MaxValue,
                new AttributeModifier(bonus, ModifierType.Percent, source: this));
        }
    }

    #endregion

    #endregion

    private Skill currentSkill;
    private Mana _heroMana;
    private float _baseAreaReconnaissanceFire;

    private int _shotCounter = 0;
    private Character _lastTarget;

    public bool IsReductionRecharge
    {
        get => _isReductionRecharge;
    }

    public bool IsElvenSkillPhysDamageHealthChance
    {
        get => _isElvenSkillPhysDamageHealthChance;
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        _skillManager = _hero.Abilities;
        _heroMana = _hero.TryGetResource(ResourceType.Mana) as Mana;

        if (_skillManager != null && _skillManager.SkillQueue != null)
            _skillManager.SkillQueue.SkillAdded += OnSkillAdded;
        if (hero != null && hero.DamageTracker != null) hero.DamageTracker.OnDamageTracked += OnDamageTracked;
        if (manaAbsorptionPhysicalTalent) hero.DamageTracker.OnDamageTracked += OnDamageDealt;
        _heroMana.ValueChanged += OnManaChanged;
        _baseAreaReconnaissanceFire = reconnaissanceFire.BaseArea;
    }

    private void OnDisable()
    {
        if (currentSkill != null)
        {
            currentSkill.CastSuccess -= ApplyCalmnessTalent;
            currentSkill.CastStarted -= ApplyFireWorshipperTalent;
        }

        if (_heroMana != null) _heroMana.ValueChanged -= OnManaChanged;
        _hero.DamageTracker.OnDamageTracked -= OnDamageDealt;
        if (_hero != null && _hero.DamageTracker != null) _hero.DamageTracker.OnDamageTracked -= OnDamageTracked;

        StopManaRegenBoost();
    }

    private void OnSkillAdded(Skill skill)
    {
        currentSkill = skill;
        if (skill == null) return;

        if (calmnessTalent) skill.CastSuccess += ApplyCalmnessTalent;

        if (fireWorshipperTalent) skill.CastStarted += ApplyFireWorshipperTalent;
    }

    #region Work with mana

    private void OnManaChanged(float oldValue, float newValue)
    {
        if (newValue > 0f) return;
        ApplyElvenSkill(newValue);
    }

    #endregion

    #region FireWorshipperTalent

    private void ApplyFireWorshipperTalent()
    {
        if (!fireWorshipperTalent || currentSkill == null) return;

        if (currentSkill.Info.DamageType != DamageType.Physical)
            return;

        var character = currentSkill.Hero;
        var targets =
            currentSkill.Targeting.GetClosestTargets(currentSkill.transform.position, currentSkill.AreaInfo.Radius);
        if (targets == null || targets.Count == 0) return;

        foreach (var target in targets)
        {
            if (target != null && target.Character.CharacterState.CheckForState(States.HuntressMark))
            {
                bool isCalmnessChance = UnityEngine.Random.Range(0f, 100f) <= calmnessChance;

                if (isCalmnessChance)
                {
                    character.CharacterState.CmdAddState(States.Calmness, durationCalmess, 0f,
                        target.Character.gameObject, currentSkill.Name);

                    if (treeRadiusCalmessTalent)
                    {
                        int treesCount = GetTreesCountInRadius(radiusTreeCalmess);
                        StartCoroutine(DelayAndUpdateCalmness(character.CharacterState, treesCount));
                    }
                }
            }
        }
    }

    #endregion

    #region treeRadiusCalmessTalent

    public void TreeRadiusCalmessTalentActive(bool value) => treeRadiusCalmessTalent = value;

    #endregion

    #region PhysicsTalent

    public void HuntressMarkPhysicsTalentActive(bool value) => huntressMarkPhysicsTalent = value;

    public void ElvenSkillPhysicsTalent(bool value)
    {
        if (elvenSkillPhysicsTalent == value) return;
        elvenSkillPhysicsTalent = value;
    }

    public void CalmnessOnElvenSkillTalent(bool value) => calmnessOnElvenSkillTalent = value;
    public void SuppressionManaAbsorption(bool value) => suppressionManaAbsorptionTalent = value;

    [Command]
    private void CmdOnDamageTracked(Damage damage, GameObject target)
    {
        OnDamageTracked(damage, target);
    }

    [Command(requiresAuthority = false)]
    public void CmdResetCooldown(Skill skill)
    {
        Debug.Log($"{skill.Name} should come off CD");
        skill.Cooldown.ForceEnd();
    }

    private void OnDamageTracked(Damage damage, GameObject target)
    {
        if (damage.Type == DamageType.Physical && _hero != null && _hero.CharacterState != null)
        {
            CharacterState selfState = _hero.CharacterState;

            if (elvenSkillPhysicsTalent && UnityEngine.Random.Range(0f, 100f) <= elvenSkillFromPhysChance)
                selfState.AddState(States.ElvenSkill, durationElvenSkill, 0f, gameObject, "TerrifyingElfAura");

            else if (calmnessOnElvenSkillTalent && selfState.CheckForState(States.ElvenSkill) &&
                     UnityEngine.Random.Range(0f, 100f) <= calmnessOnElvenSkillChance)
                selfState.AddState(States.Calmness, durationCalmess, 0f, gameObject, "TerrifyingElfAura");

            //if (huntressMarkPhysicsTalent && UnityEngine.Random.Range(0f, 100f) <= huntressMarkApplyChance && target != null && target.TryGetComponent<CharacterState>(out var victimState))
            //    victimState.AddState(States.HuntressMark, durationHuntressMark, 0f, gameObject, "HuntressMark");

            if (suppressionManaAbsorptionTalent &&
                selfState.GetState(States.Suppression) is SuppressionState suppression &&
                suppression.CurrentStacksCount > 0 && _hero.TryGetResource(ResourceType.Mana) is Mana mana)
                mana.Add(damage.Value * 0.25f * suppression.CurrentStacksCount);

            if (manaAbsorptionPhysicalTalent) OnDamageDealt(damage, target);

            if (_isSpellAddInnerDarkness && damage.Type == DamageType.Magical && target != null &&
                target.TryGetComponent<Character>(out var targetCharacter))
            {
                float roll = UnityEngine.Random.Range(0f, 100f);
                if (roll <= innerDarknessChance)
                    targetCharacter.CharacterState.AddState(States.InnerDarkness, durationCalmess, 0f, _hero.gameObject,
                        "TerrifyingElfAura");
            }
        }
    }

    public void ResetCoolDown(Skill skill)
    {
        if (isServer) skill.Cooldown.ForceEnd();
        else CmdResetCooldown(skill);
    }

    public void ProcessShot(Character target)
    {
        if (!_isThirdShotRow) return;
        if (target == null) return;

        ProcessShotThird(target);
    }

    private void ProcessShotThird(Character target)
    {
        if (_lastTarget != target)
        {
            _lastTarget = target;
            _shotCounter = 0;
        }

        _shotCounter++;

        if (_shotCounter >= 3)
        {
            _shotCounter = 0;

            foreach (var skill in _skillManager.Skills)
            {
                if (skill == null) continue;

                ResetCoolDown(skill);
            }
        }
    }

    #endregion

    #region ManaAbsorptionPhysicalTalent

    public void ManaAbsorptionPhysicalTalentActive(bool value)
    {
        manaAbsorptionPhysicalTalent = value;
    }

    private void OnDamageDealt(Damage damage, GameObject target)
    {
        if (damage.Type == DamageType.Physical && _hero != null)
        {
            float manaToRestore = damage.Value * 0.3f;
            RestoreMana(manaToRestore, target);
        }
    }

    private void RestoreMana(float amount, GameObject target)
    {
        if (target != null && target.TryGetComponent<Character>(out var targetCharacter))
        {
            if (targetCharacter.TryGetResource(ResourceType.Mana) is Mana targetManaResource)
            {
                float manaToReduce = Mathf.Min(amount, targetManaResource.CurrentValue);
                if (manaToReduce > 0) targetManaResource.TryUse(amount);

                else return;
            }
        }

        if (_hero.TryGetResource(ResourceType.Mana) is Mana manaResource) manaResource.Add(amount);
    }

    #endregion

    #region The Elf has run out of mana

    private void ApplyElvenSkill(float newValue)
    {
        if (!elvenSkillTalent || _hero == null || _hero.CharacterState == null || newValue > 0) return;

        if (isClient)
            _hero.CharacterState.CmdAddState(States.ElvenSkill, durationElvenSkill, 0f, gameObject,
                "TerrifyingElfAura");
    }

    public void ElvenSkillTalent(bool value)
    {
        if (value == elvenSkillTalent) return;
        elvenSkillTalent = value;
    }

    #endregion

    #region Helpers

    private int GetTreesCountInRadius(float radius)
    {
        var trees = FindObjectsOfType<Tree>();
        int count = 0;
        foreach (var t in trees)
        {
            if (Vector3.Distance(t.transform.position, transform.position) <= radius)
            {
                count++;
            }
        }

        return count;
    }

    private IEnumerator DelayAndUpdateCalmness(CharacterState targetState, int treesCount)
    {
        yield return null;

        //if (!isServer) yield break;

        var calmness = targetState.GetState(States.Calmness) as Calmness;
        if (calmness != null)
        {
            calmness.UpdateTreesCount(treesCount);
        }
    }

    #endregion

    #region Skill

    protected override IEnumerator CastJob()
    {
        yield break;
    }

    protected override int AnimTriggerCastDelay { get; }
    protected override int AnimTriggerCast { get; }

    #endregion

    #region ManaIncreasedRegen

    private bool _isManaRegenIncreased;
    private Coroutine _manaWatchRoutine;
    private Coroutine _manaRegenRoutine;

    private const float ManaRegenBoostedValue = 100f;
    private const float ManaRegenIdleTime = 6f;
    private const float ManaWatchInterval = 0.1f;

    public void IncreaseManaRegen(bool value)
    {
        if (_isManaRegenIncreased == value) return;
        _isManaRegenIncreased = value;

        if (_isManaRegenIncreased)
        {
            if (_manaWatchRoutine != null) StopCoroutine(_manaWatchRoutine);
            _manaWatchRoutine = StartCoroutine(ManaWatcher());
        }
        else
        {
            StopManaRegenBoost();
        }
    }

    private IEnumerator ManaWatcher()
    {
        var wait = new WaitForSeconds(ManaWatchInterval);
        float prevMana = GetCurrentMana();

        if (_manaRegenRoutine != null) StopCoroutine(_manaRegenRoutine);
        _manaRegenRoutine = StartCoroutine(ManaRegenWatcher());

        while (_isManaRegenIncreased)
        {
            yield return wait;

            float currentMana = GetCurrentMana();

            if (currentMana < prevMana - 0.01f)
            {
                ApplyManaRegenBoost(false);

                if (_manaRegenRoutine != null) StopCoroutine(_manaRegenRoutine);
                _manaRegenRoutine = StartCoroutine(ManaRegenWatcher());
            }

            prevMana = currentMana;
        }

        _manaWatchRoutine = null;
    }

    private IEnumerator ManaRegenWatcher()
    {
        yield return new WaitForSeconds(ManaRegenIdleTime);
        ApplyManaRegenBoost(true);
        _manaRegenRoutine = null;
    }

    private void ApplyManaRegenBoost(bool active)
    {
        if (Hero.TryGetResource(ResourceType.Mana) is not Resource mana) return;

        mana.RemoveModifierBySource(ResourceAttributeName.Regen, this, all: true);

        if (active)
        {
            float baseRegen = mana.Attr_RegenValue?.GetValue() ?? 0f;
            float flatBonus = ManaRegenBoostedValue - baseRegen;
            if (flatBonus > 0f)
                mana.AddModifier(ResourceAttributeName.Regen,
                    new AttributeModifier(flatBonus, ModifierType.Flat, source: this));
        }
    }

    private float GetCurrentMana()
    {
        return Hero.TryGetResource(ResourceType.Mana) is Resource mana ? mana.CurrentValue : 0f;
    }

    private void StopManaRegenBoost()
    {
        if (_manaWatchRoutine != null)
        {
            StopCoroutine(_manaWatchRoutine);
            _manaWatchRoutine = null;
        }

        if (_manaRegenRoutine != null)
        {
            StopCoroutine(_manaRegenRoutine);
            _manaRegenRoutine = null;
        }

        ApplyManaRegenBoost(false);
    }

    #endregion

}
