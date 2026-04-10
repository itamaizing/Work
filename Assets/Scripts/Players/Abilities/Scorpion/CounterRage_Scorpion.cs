using Mirror;
using UnityEngine;

public class CounterRage_Scorpion : NetworkBehaviour
{
    [SerializeField] private float _baseChance = 0.8f;
    private Character _character;
    private bool _counterRageIsEnabled = false;
    private float _maxPossibleBonus = 0;
    
    public void EnableCounterRage(bool value, Character character)
    {
        _character = character;
        if (_maxPossibleBonus == 0)
        {
            _maxPossibleBonus = _character.TryGetResource(ResourceType.Energy).MaxValue * 0.3f;
        }
        if(_counterRageIsEnabled == value) return;
        _counterRageIsEnabled = value;
        EnableRage(character, _counterRageIsEnabled);

    }

    private void EnableRage(Character character, bool value)
    {
        if (value)
            character.Health.DamageTaken += OnDamageTaken;
        else
            character.Health.DamageTaken -= OnDamageTaken;
    }

    private void OnDamageTaken(Damage damage, Skill from)
    {
        if (!isOwned) return;
        if (Random.value > _baseChance) return;

        float bonus = damage.Value * 0.5f;
        CmdAddRageState(bonus);
    }

    [Command]
    private void CmdAddRageState(float bonus)
    {
        _character.CharacterState.AddState(States.CounterRage, 3f, bonus, _character.gameObject, nameof(CounterRage_Scorpion));
    }
}
