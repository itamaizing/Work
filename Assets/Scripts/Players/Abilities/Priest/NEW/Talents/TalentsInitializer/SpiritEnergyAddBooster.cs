using Mirror;
using UnityEngine;

public class SpiritEnergyAddBooster : SkillTalentHandler
{
    private bool _enabled;
    private States state;
    private float time;

    public SpiritEnergyAddBooster(NetworkBehaviour owner) : base(owner) { }

    public void Enable(bool value) => _enabled = value;

    public bool TryApply(GameObject targetGo, bool isLightMode,float buffDuration,float debuffDuration, out States outState, out float outTime)
    {
        outState = States.SpiritHealth;
        outTime = 0;
        if (!_enabled || targetGo == null) return false;

        var stateComponent = targetGo.GetComponent<CharacterState>();
        if (stateComponent == null) return false;

        outState = isLightMode ? States.SpiritEnergy : States.SpiritHealth;
        outTime = isLightMode ? buffDuration : debuffDuration;

        return true;
    }
}
