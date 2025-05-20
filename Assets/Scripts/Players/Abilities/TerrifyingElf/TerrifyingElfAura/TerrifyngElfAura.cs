using Mirror;
using UnityEngine;
using System.Collections;
using System;

public class TerrifyingElfAura : Skill
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private bool calmnessTalent;
    [SerializeField] private bool fireWorshipperTalent;
    [SerializeField] private bool treeRadiusCalmessTalent;
    [SerializeField] private bool huntressMarkPhysicsTalent;
    [SerializeField] private bool manaAbsorptionPhysicalTalent;
    [SerializeField, Range(0f, 100f)] private float calmnessChance = 10f;
    [SerializeField, Range(0f, 100f)] private float huntressMarkApplyChance = 5f;
    [SerializeField] private float durationCalmess;
    [SerializeField] private float durationHuntressMark;

    private Skill currentSkill;
    #region Skill
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => false;
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved) { yield break; }
    protected override IEnumerator CastJob() { yield break; }
    protected override void ClearData() { }
    #endregion

    private void OnEnable()
    {
        if (skillManager != null && skillManager.SkillQueue != null)
        {
            skillManager.SkillQueue.SkillAdded += OnSkillAdded;
        }

        if (Hero != null && Hero.DamageTracker != null) Hero.DamageTracker.OnDamageTracked += OnDamageTracked;
        if (manaAbsorptionPhysicalTalent) Hero.DamageTracker.OnDamageTracked += OnDamageDealt;
    }

    private void OnDisable()
    {
        if (currentSkill != null)
        {
             currentSkill.CastSuccess -= ApplyCalmnessTalent;
             currentSkill.CastStarted -= ApplyFireWorshipperTalent;
        }

         Hero.DamageTracker.OnDamageTracked -= OnDamageDealt;
        if (Hero != null && Hero.DamageTracker != null) Hero.DamageTracker.OnDamageTracked -= OnDamageTracked;
    }

    private void OnSkillAdded(Skill skill)
    {
        currentSkill = skill;
        if (skill == null) return;

        if (calmnessTalent) skill.CastSuccess += ApplyCalmnessTalent;

        if (fireWorshipperTalent) skill.CastStarted += ApplyFireWorshipperTalent;
    }

    #region CalmnessTalent

    public void CalmnessTalentActive(bool value)
    {
        calmnessTalent = value;
    }

    private void ApplyCalmnessTalent()
    {
        if (!calmnessTalent || currentSkill == null) return;

        if (currentSkill.AbilityForm == AbilityForm.Spell)
        {
            var character = currentSkill.Hero;
            if (character != null && character.CharacterState != null)
            {
                bool isCalmnessChance = UnityEngine.Random.Range(0f, 100f) <= calmnessChance;

                if (isCalmnessChance)
                {
                    character.CharacterState.CmdAddState(States.Calmness, durationCalmess, 0f, this.gameObject, currentSkill.Name);

                    if (treeRadiusCalmessTalent)
                    {
                        int treesCount = GetTreesCountInRadius(Radius);
                        StartCoroutine(DelayAndUpdateCalmness(character.CharacterState, treesCount));
                    }
                }
            }
        }

        currentSkill = null;
    }

    #endregion

    #region FireWorshipperTalent

    public void FireWorshipperTalentActive(bool value)
    {
        fireWorshipperTalent = value;
    }

    private void ApplyFireWorshipperTalent()
    {
        if (!fireWorshipperTalent || currentSkill == null) return;

        if (currentSkill.DamageType != DamageType.Physical)
            return;

        var character = currentSkill.Hero;
        var targets = currentSkill.GetCloserTargets(currentSkill.transform.position, currentSkill.Radius);
        if (targets == null || targets.Count == 0) return;

        foreach (var target in targets)
        {
            if (target != null && target.CharacterState.CheckForState(States.HuntressMark))
            {
                bool isCalmnessChance = UnityEngine.Random.Range(0f, 100f) <= calmnessChance;

                if (isCalmnessChance)
                {
                    character.CharacterState.CmdAddState(States.Calmness, durationCalmess, 0f, target.gameObject, currentSkill.Name);

                    if (treeRadiusCalmessTalent)
                    {
                        int treesCount = GetTreesCountInRadius(Radius);
                        StartCoroutine(DelayAndUpdateCalmness(character.CharacterState, treesCount));
                    }
                }
            }
        }
    }

    #endregion

    #region treeRadiusCalmessTalent

    public void TreeRadiusCalmessTalentActive(bool value)
    {
        treeRadiusCalmessTalent = value;
    }

    #endregion

    #region HuntressMarkPhysicsTalent

    public void HuntressMarkPhysicsTalentActive(bool value)
    {
        huntressMarkPhysicsTalent = value;
    }

    private void OnDamageTracked(Damage damage, GameObject target)
    {
        if (!huntressMarkPhysicsTalent) return;

        if (damage.Type != DamageType.Physical) return;

        bool chance = UnityEngine.Random.Range(0f, 100f) <= huntressMarkApplyChance;
        if (!chance) return;

        if (target.TryGetComponent<CharacterState>(out var characterState))
            characterState.AddState(States.HuntressMark, durationHuntressMark, 0f, this.gameObject, "HuntressMark");
    }

    #endregion

    #region ManaAbsorptionPhysicalTalent

    public void ManaAbsorptionPhysicalTalentActive(bool value)
    {
        manaAbsorptionPhysicalTalent = value;
    }

    private void OnDamageDealt(Damage damage, GameObject target)
    {
        if (damage.Type == DamageType.Physical && Hero != null)
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

        if (Hero.TryGetResource(ResourceType.Mana) is Mana manaResource) manaResource.Add(amount);
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

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        throw new NotImplementedException();
    }
}
