using System.Collections;
using UnityEngine;

public class GodAuraStackingTalent : Talent
{
    [SerializeField] private float _stackChance = 30f;
    [SerializeField] private float _stackDuration = 3f;

    private int _activeStacks = 0;
    private Coroutine _stackCoroutine;

    public override void Enter()
    {
        character.Health.DamageTaken += OnDamageTaken;
    }

    public override void Exit()
    {
        character.Health.DamageTaken -= OnDamageTaken;

        if (_stackCoroutine != null)
        {
            character.StopCoroutine(_stackCoroutine);
            _stackCoroutine = null;
        }
        _activeStacks = 0;
    }

    private void OnDamageTaken(Damage damage, Skill skill)
    {
        if (_activeStacks >= 3) return;
        if (Random.Range(0f, 100f) > _stackChance) return;

        var godAura = character.CharacterState.GetState(States.GodAura) as GodAura;
        if (godAura == null) return;

        _activeStacks++;
        godAura.AddTalentStack();

        if (_stackCoroutine != null)
            character.StopCoroutine(_stackCoroutine);

        _stackCoroutine = character.StartCoroutine(StacksExpireCoroutine(godAura));
    }

    private IEnumerator StacksExpireCoroutine(GodAura godAura)
    {
        while (_activeStacks > 0)
        {
            yield return new WaitForSeconds(_stackDuration);

            if (_activeStacks <= 0) break;

            _activeStacks--;
            godAura.RemoveTalentStack();
        }

        godAura.ResetToBaseAura();

        _stackCoroutine = null;
    }
}
