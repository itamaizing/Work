using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GrowTreeAura : NetworkBehaviour
{
    private class SkillBaseData
    {
        public float length;
        public float radius;
    }

    [Header("Tick")]
    [SerializeField] private float _tick = 1f;
    [SerializeField] private LayerMask characterLayer;

    private readonly List<Character> charactersInZone = new();
    private readonly HashSet<uint> clientIds = new();
    private Coroutine _routine;
    private const float TreeMultiplier = 1.2f;
    private const float VisionBonus = 3f;
    private const float TreeBuffPercent = 1.5f;

    private readonly Dictionary<Skill, SkillBaseData> _baseValues = new();
    private bool _isBuffApplied = false;

    [SerializeField][ReadOnly] private SkillManager _skillManager;
    [SerializeField] [ReadOnly] private Character _Hero;

    [Header("Talent")]
    private bool _growTreeIncreasesMaxHealth;
    
    public bool GrowTreeIncreasesMaxHealth { get => _growTreeIncreasesMaxHealth; set => _growTreeIncreasesMaxHealth = value; }

    public void Init(SkillManager skill, Character hero)
    {
        _skillManager = skill;
        _Hero = hero;
    }
    
    public void EnableTreeManaRegen(bool isEnable)
    {
        _treeManaRegenTalent = isEnable;

        EnableManaRegen(_treeManaRegenTalent);
    }

    private void EnableManaRegen(bool value)
    {
        if (value)
        {
            if(_manaRegenRoutine == null)
                _manaRegenRoutine = StartCoroutine(ApplyTreeManaRegenPeriodically());
        }
        else
        {
            if (_manaRegenRoutine != null)
            {
                StopCoroutine(_manaRegenRoutine);
                _manaRegenRoutine = null;
            }
            if (_Hero.TryGetResource(ResourceType.Mana) is Mana manaResource)
            {
                ResetTreeManaModifier(manaResource);
            }
        }
    }

    [Server]
    private void RemoveAuthority()
    {
        var id = netIdentity;
        if (id.connectionToClient != null) id.RemoveClientAuthority();
    }

    private void OnDestroy()
    {
        RemoveAuthority();
        if (_routine != null) StopCoroutine(_routine);


        foreach (var character in charactersInZone) ForceExit(character);
        foreach (var id in clientIds.ToArray()) RemoveCharacter(id);

        charactersInZone.Clear();
        clientIds.Clear();
        if (_Hero != null) RemoveTreeBuff(_Hero);
        
        if (_manaRegenRoutine != null)
        {
            StopCoroutine(_manaRegenRoutine);
            _manaRegenRoutine = null;
        }

        if (_Hero != null)
        {
            if (_Hero.TryGetResource(ResourceType.Mana) is Mana manaResource)
            {
                ResetTreeManaModifier(manaResource);
            }
        }
    }

    public void ApplyTreeBuff(Character character)
    {
        if (_skillManager == null) return;
        if (_Hero != character) return;

        RemoveTreeBuff(character);

        character.VisionComponent.VisionRange += VisionBonus;

        foreach (var skill in _skillManager.Abilities)
        {
            if (skill == null) continue;

            skill.Attributes[SkillAttributeName.Length].AddModifier(new AttributeModifier(TreeBuffPercent, ModifierType.Flat, source: this));
            skill.Attributes[SkillAttributeName.Radius].AddModifier(new AttributeModifier(TreeBuffPercent, ModifierType.Flat, source: this));
        }
    }

    public void RemoveTreeBuff(Character character)
    {
        if (_skillManager == null) return;
        if (_Hero != character) return;

        character.VisionComponent.VisionRange -= VisionBonus;

        foreach (var skill in _skillManager.Abilities)
        {
            if (skill == null) continue;

            skill.Attributes[SkillAttributeName.Length].RemoveBySource(this, all: true);
            skill.Attributes[SkillAttributeName.Radius].RemoveBySource(this, all: true);
        }
    }

    private void ForceExit(Character character)
    {
        if (character == null) return;
        if (character.TryGetComponent<CharacterState>(out var state) && state.GetState(States.ShadowTree) is ShadowTree shadow) shadow.SwitchToFinite();
    }

    private void CacheBaseValues()
    {
        _baseValues.Clear();

        foreach (var skill in _skillManager.Abilities)
        {
            if (skill == null) continue;

            _baseValues[skill] = new SkillBaseData
            {
                length = skill.Buff.Length.GetBuffedValue(1f),
                radius = skill.Buff.Radius.GetBuffedValue(1f)
            };
        }
    }

    private void ApplyTreeBuffLocal()
    {
        if (_skillManager == null || _Hero == null) return;
        if (_isBuffApplied) return;

        CacheBaseValues();

        _Hero.VisionComponent.VisionRange += VisionBonus;

        foreach (var skill in _skillManager.Abilities)
        {
            if (skill == null) continue;

            skill.Buff.Length.IncreasePercentage(TreeMultiplier);
            skill.Buff.Radius.IncreasePercentage(TreeMultiplier);
        }

        _isBuffApplied = true;
    }

    private void RemoveTreeBuffLocal()
    {
        if (_skillManager == null || _Hero == null) return;
        if (!_isBuffApplied) return;

        _Hero.VisionComponent.VisionRange -= VisionBonus;

        foreach (var skill in _skillManager.Abilities)
        {
            if (skill == null) continue;

            if (_baseValues.TryGetValue(skill, out var data))
            {
                skill.Buff.Length.SetBaseValue(data.length);
                skill.Buff.Radius.SetBaseValue(data.radius);
            }
        }

        _isBuffApplied = false;
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (!_growTreeIncreasesMaxHealth) return;
        if (((1 << other.gameObject.layer) & characterLayer.value) == 0) return;

        if (other.TryGetComponent<Character>(out Character character) && !charactersInZone.Contains(character))
        {
            charactersInZone.Add(character);
            RpcAddCharacter(character.netId);
            if (_routine == null) _routine = StartCoroutine(ApplyPartialShadowTreePeriodically());
        }
    }

    [ServerCallback]
    private void OnTriggerExit(Collider other)
    {
        if (!_growTreeIncreasesMaxHealth) return;
        if (((1 << other.gameObject.layer) & characterLayer.value) == 0) return;

        if (other.TryGetComponent<Character>(out Character character))
        {
            charactersInZone.Remove(character);
            ForceExit(character);
            RpcRemoveCharacter(character.netId);

            if (charactersInZone.Count == 0 && _routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }
    }

    private IEnumerator ApplyPartialShadowTreePeriodically()
    {
        var wait = new WaitForSeconds(_tick);

        while (charactersInZone.Count > 0)
        {
            foreach (Character character in charactersInZone)
            {
                if (character == null || !character.TryGetComponent(out CharacterState state)) continue;
                state.AddState(States.ShadowTree, 9999, 0f, gameObject, name);
            }

            yield return wait;
        }

        _routine = null;
    }

    [ClientRpc] public void RpcApplyTreeBuff(Character character) => ApplyTreeBuff(character);
    [ClientRpc] public void RpcRemoveTreeBuff(Character character) => RemoveTreeBuff(character);

    [ClientRpc]
    private void RpcAddCharacter(uint netId)
    {
        if (!NetworkClient.spawned.TryGetValue(netId, out var id)) return;
        if (!clientIds.Add(netId)) return;
    }

    [ClientRpc] private void RpcRemoveCharacter(uint netId) => RemoveCharacter(netId);

    private void RemoveCharacter(uint netId)
    {
        if (!clientIds.Remove(netId)) return;

        if (NetworkClient.spawned.TryGetValue(netId, out var id) &&
            id.TryGetComponent(out CharacterState state))
        {
            (state.GetState(States.ShadowTree) as ShadowTree)?.SwitchToFinite();
        }
    }

    #region TreeManaRegenTalent

    private bool _treeManaRegenTalent;
    private Coroutine _manaRegenRoutine;
    
    private const float ManaRegenRadius = 12f;
    private const float MaxManaBonusPercent = 0.15f;
    private const int MaxGrowthTicks = 10;
    
    private float _currentAddedPercent = 0f;
    private int _currentGrowthTicks = 0;

    private IEnumerator ApplyTreeManaRegenPeriodically()
    {
        var wait = new WaitForSeconds(1f);
        var mana = _Hero != null ? _Hero.TryGetResource(ResourceType.Mana) : null;

        while (_Hero != null && mana != null)
        {
            float distance = Vector3.Distance(transform.position, _Hero.transform.position);
            if (distance <= ManaRegenRadius)
            {
                if (_currentGrowthTicks < MaxGrowthTicks)
                {
                    mana.RemoveModifierBySource(ResourceAttributeName.MaxValue, this, all: true);

                    _currentGrowthTicks++;
                    _currentAddedPercent = (_currentGrowthTicks / (float)MaxGrowthTicks) * MaxManaBonusPercent;

                    mana.AddModifier(ResourceAttributeName.MaxValue, 
                        new AttributeModifier(_currentAddedPercent, ModifierType.Percent, source: this));
                }
            }
            else
            {
                if (_currentGrowthTicks > 0)
                {
                    ResetTreeManaModifier(mana);
                }
            }

            yield return wait;
        }

        _manaRegenRoutine = null;
    }

    private void ResetTreeManaModifier(Resource manaResource)
    {
        if (manaResource == null || _currentGrowthTicks == 0) return;
        
        manaResource.RemoveModifierBySource(ResourceAttributeName.MaxValue, this, all: true);

        _currentGrowthTicks = 0;
        _currentAddedPercent = 0f;
    }

    #endregion
}
