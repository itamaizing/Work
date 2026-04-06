using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class DestructionFillingBooster : SkillTalentHandler
{
    private bool _enabled;
    private float _extensionTime;
    private float _duration;
    private float _chance;

    private States state;
    private float time;

    public DestructionFillingBooster(NetworkBehaviour owner) : base(owner) { }

    public void Enable(bool value, float duration, float extensionTime, float chance)
    {
        _enabled = value;
        _duration = duration;
        _extensionTime = extensionTime;
        _chance = chance;
    }

    public bool TryApply(GameObject targetGo, bool isLightMode, out States state, out float time)
    {
        state = States.Destruction;
        time = 0f;

        if (!_enabled || targetGo == null || Random.value > _chance)
            return false;

        States stateToUse = isLightMode
            ? (_stackingRestoration ? States.RestorationStacking : States.Restoration)
            : (_stackingDestruction ? States.DestructionStacking : States.Destruction);

        float durationToApply = targetGo.GetComponent<Character>().CharacterState.CheckForState(stateToUse) ? _extensionTime : _duration;

        state = stateToUse;
        time = durationToApply;
        return true;
    }

    private bool _stackingRestoration;
    private bool _stackingDestruction;

    public void SetStackingRestoration(bool value) => _stackingRestoration = value;
    public void SetStackingDestruction(bool value) => _stackingDestruction = value;
}
