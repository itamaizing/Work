using Mirror;
using UnityEngine;

public class SoulTiredDispelBooster : SkillTalentHandler
{
    private bool _enabled;
    public bool Enabled => _enabled;

    public SoulTiredDispelBooster(NetworkBehaviour owner) : base(owner) { }

    public void Enable(bool value) => _enabled = value;
    
    public bool CanCastOnTarget(Character target)
    {
        if (!_enabled || target == null) 
            return false;

        bool isSelf = target == Owner.GetComponent<Character>();
        bool isAlly = target.gameObject.layer == LayerMask.NameToLayer("Allies");
        bool hasTiredSoul = target.CharacterState.CheckForState(States.TiredSoul);
        return (isSelf || isAlly) && hasTiredSoul;
    }

    public void TryRemoveTiredSoul(GameObject target,bool enabled)
    {
        Debug.LogError("Removing...");
        if (!enabled || target == null) 
            return;
        Debug.LogError("Is Enabled");
        var characterState = target.GetComponent<Character>().CharacterState;
        if (characterState == null) 
            return;

        Debug.LogError("Character not null");
        
        if (characterState.CheckForState(States.TiredSoul))
        {
            Debug.LogError("removing cmd");
            characterState.RemoveState(States.TiredSoul);
        }
    }
}
