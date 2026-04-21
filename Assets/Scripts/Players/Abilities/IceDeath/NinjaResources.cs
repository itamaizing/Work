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

    #region Talent
    private bool _isIceRuneTalent;
    private bool _isHardenedFleshTalent;
    private bool _isFrozenCrit;

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

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }


    private void OnDisable()
    {
        Hero.DamageTracker.OnDamageTracked -= OnDamageTaken;
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
        Hero.DamageTracker.OnDamageTracked += OnDamageTaken;
        Hero.Health.DamageTaken += HandleDamageTaken;

        if (Hero.TryGetResource(ResourceType.Rune) is RuneComponent rune)
        {
            rune.OnRuneSpent += OnRuneSpent;
        }
    }

    private void OnDamageTaken(Damage damage, GameObject attacker)
    {
        if (_isIceRuneTalent && damage.Value > 0  && Hero.TryGetResource(ResourceType.Energy) is Energy energy)
        {
            float energyToRestore = damage.Value * 0.2f;
            energy.Add(energyToRestore);
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
}
