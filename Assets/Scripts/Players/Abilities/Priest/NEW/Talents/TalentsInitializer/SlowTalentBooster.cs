using Mirror;
using UnityEngine;

public class SlowTalentBooster : SkillTalentHandler
{
    private bool _enabled;
    private const float SlowAmount = 0.6f;

    public SlowTalentBooster(NetworkBehaviour owner) : base(owner) { }

    public void Enable(bool value) => _enabled = value;

    public void TryApplySlow(Character target)
    {
        if (!_enabled || target == null) return;
        if (Owner.GetComponent<Character>().CharacterState.GetState(States.DarkFormState) == null) return;

        target.CharacterState.AddState(States.SlowFlowLight, 4f, 0, Owner.gameObject, "SlowTalent");
    }

    public void TryRemoveSlow(Character target)
    {
        if (!_enabled || target == null) return;
        target.CharacterState.RemoveState(States.SlowFlowLight);
    }
}
