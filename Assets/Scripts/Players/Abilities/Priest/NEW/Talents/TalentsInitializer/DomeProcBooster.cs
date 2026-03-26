using Mirror;
using UnityEngine;

public class DomeProcBooster : SkillTalentHandler
{
    private bool _enabled;
    private readonly float _procChance = 30f;
    private readonly DomeOfLight _domeSkill;

    public DomeProcBooster(NetworkBehaviour owner, DomeOfLight domeSkill) : base(owner)
    {
        _domeSkill = domeSkill;
    }

    public void Enable(bool value)
    {
        _enabled = value;
    }


    public void TryProcFromHeal(Character healedTarget)
    {
        if (!_enabled || _domeSkill == null || !Owner.isOwned) 
            return;

        if (healedTarget == null || healedTarget.IsDead) 
            return;

        if (Random.value * 100f >= _procChance) 
            return;

        _domeSkill.CmdSpawnTemporaryDome(healedTarget.transform.position);
    }
}
