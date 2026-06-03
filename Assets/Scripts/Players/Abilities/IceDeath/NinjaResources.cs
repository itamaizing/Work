using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class NinjaResources : Skill, IPassiveSkill
{
    #region Skill
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => false;
    protected override IEnumerator CastJob() => null;
    protected override void ClearData() { }
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved) => null;
    public override void LoadTargetData(TargetInfo targetInfo) => throw new NotImplementedException();
    #endregion

    private float _accumulatedDamageForRune;
    private const float DamagePerRune = 100f;
    private const float EnergyRestorePercent = 0.2f;

    #region Talent
    private bool _isIceRuneTalent;
    private bool _isHardenedFleshTalent;
    private bool _isFrozenCrit;
    private bool _isRepeatedFrost;
    private bool _isRuneRegenSpeed;

    public void RepeatedFrost(bool value) => _isRepeatedFrost = value;
    public void RuneRegenSpeed(bool value) => _isRuneRegenSpeed = value;

    public bool IsRepeatedFrost { get => _isRepeatedFrost; set => _isRepeatedFrost = value; }

    public void FrozenCrit(bool value) => _isFrozenCrit = value;

    /*  private void Update()
      {
          if(Input.GetKeyDown(KeyCode.T))
          {
              Hero.CharacterState.CmdAddState(States.HardenedFlesh, 9f, 0, Hero.gameObject, this.Name);
          }
      }*/

    public void EnergyToRestore(bool value, string text)
    {
        _isIceRuneTalent = value;
        //AbilityInfoHero.FinalDescription = value ? AbilityInfoHero.Description + $" {text}" : AbilityInfoHero.Description;
    }

    public void HardenedFleshTalent(bool value, string text)
    {
        _isHardenedFleshTalent = value;
        //AbilityInfoHero.FinalDescription = value ? AbilityInfoHero.Description + $" {text}" : AbilityInfoHero.Description;
    }
    #endregion

    private Coroutine _regenRoutine;

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        TrySubscribe();

        if (isServer) _regenRoutine = StartCoroutine(UpdateRuneRegenRoutine());
    }

    private void OnEnable()
    {
        TrySubscribe();
    }


    private void OnDisable()
    {
        Hero.DamageTracker.OnDamageTracked -= OnDealDamage;
        Hero.Health.DamageTaken -= HandleDamageTaken;

        if (Hero.TryGetResource(ResourceType.Rune) is RuneComponent rune)
        {
            rune.OnRuneSpent -= OnRuneSpent;
        }
    }

    private void ModifyFrozenCrit(Character targetCharacter, ref Damage damage, Skill skill)
    {
        if (!_isFrozenCrit) return;
        if (targetCharacter == null || skill == null) return;
        if (skill.Hero != Hero) return;
        if (skill is not IceSword) return;
        if (!targetCharacter.CharacterState.CheckForState(States.Frozen))  return;

        damage.Value *= 1.10f;

        if (UnityEngine.Random.Range(0f, 100f) < 30f)
        {
            float critMultiplier = UnityEngine.Random.Range(1.8f, 2.3f);
            damage.Value *= critMultiplier;
        }
    }

    private void TrySubscribe()
    {
        if (Hero == null)
        {
            //Debug.LogError("Hero was not initialized yet", gameObject);
            return;
        }
        //Debug.LogError("Hero was initialized", gameObject);
        Hero.DamageTracker.OnDamageTracked += OnDealDamage;
        Hero.Health.DamageTaken += HandleDamageTaken;

        if (Hero.TryGetResource(ResourceType.Rune) is RuneComponent rune)
        {
            rune.OnRuneSpent += OnRuneSpent;
        }
    }

    private void OnDealDamage(Damage damage, GameObject target)
    {
        if (!_isIceRuneTalent)
            return;

        if (!isServer)
            return;

        if (damage.Value <= 0)
            return;

        if (Hero.TryGetResource(ResourceType.Energy) is Energy energy)
        {
            energy.Add(damage.Value * EnergyRestorePercent);
        }

        _accumulatedDamageForRune += damage.Value;

        while (_accumulatedDamageForRune >= DamagePerRune)
        {
            _accumulatedDamageForRune -= DamagePerRune;

            if (Hero.TryGetResource(ResourceType.Rune) is RuneComponent rune)
            {
                rune.CmdAdd(1);
            }
        }

        RestoreResourcesToAllies(damage.Value);
    }

    private void RestoreResourcesToAllies(float damageValue)
    {
        float restoreValue = damageValue * EnergyRestorePercent;

        foreach (Character character in FindObjectsOfType<Character>())
        {
            if (character == null) continue;
            if (character == Hero) continue;
            if (character.gameObject.layer != Hero.gameObject.layer) continue;

            if (Vector3.Distance(Hero.transform.position, character.transform.position) > AreaInfo.Radius) continue;

            if (character.TryGetResource(ResourceType.Energy) is Energy energy) energy.Add(restoreValue);
            if (character.TryGetResource(ResourceType.Mana) is Mana mana) mana.Add(restoreValue);
        }
    }

    private void HandleDamageTaken(Damage damage, Skill skill)
    {
        if (_isHardenedFleshTalent && damage.Type == DamageType.Physical && damage.Value > 0)
        {
            for (int i = 0; i < damage.Value; i++)
            {
                float roll = UnityEngine.Random.Range(0f, 1f);
                if (roll <= 0.01f)
                {
                    Hero.CharacterState.CmdAddState(States.HardenedFlesh, 9f, 0, Hero.gameObject, this.Name);
                    break;
                }
            }
        }
    }

    private void OnRuneSpent(float value, Skill usedSkill)
    {
        if (!isServer) return;
        if (value <= 0) return;

        if (Hero.CharacterState.CheckForState(States.FrostEnergy)) Hero.CharacterState.RemoveState(States.FrostEnergy);
    }

    private float CalculateRuneRegenBonus()
    {
        float totalBonus = 0f;

        foreach (var character in FindObjectsOfType<Character>())
        {
            if (character == null) continue;

            var state = character.CharacterState;
            if (state == null) continue;

            bool isHero = character.GetComponent<HeroComponent>() != null;
            bool isMinion = character.GetComponent<MinionComponent>() != null;

            if (state.CheckForState(States.Frozen))
            {
                if (isHero) totalBonus += 0.20f;
                else if (isMinion) totalBonus += 0.10f;
            }
            else if (state.CheckForState(States.Frosting))
            {
                if (isHero) totalBonus += 0.10f;
                else if (isMinion) totalBonus += 0.05f;
            }
        }

        return totalBonus;
    }

    private IEnumerator UpdateRuneRegenRoutine()
    {
        while (true)
        {
            if (_isRuneRegenSpeed)
            {
                float bonus = CalculateRuneRegenBonus();

                if (Hero.TryGetResource(ResourceType.Rune) is RuneComponent rune) rune.SetExternalRegenMultiplier(1f + bonus);
            }
            else
            {
                if (Hero.TryGetResource(ResourceType.Rune) is RuneComponent rune) rune.SetExternalRegenMultiplier(1f);
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
}
