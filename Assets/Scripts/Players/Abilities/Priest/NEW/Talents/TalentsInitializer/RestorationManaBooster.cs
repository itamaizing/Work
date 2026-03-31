using Mirror;

public class RestorationManaBooster : SkillTalentHandler
{
    private bool _enabled;

    public RestorationManaBooster(NetworkBehaviour owner) : base(owner) { }

    public override void Enable(bool value)
    {
        if (_enabled == value) return;
        _enabled = value;
    }

    public void OnRestorationTick(float healAmount, Character target)
    {
        if (!_enabled || !Owner.isOwned || healAmount <= 0f)
            return;

        var mana = target.TryGetResource(ResourceType.Mana);
        if (mana != null)
        {
            mana.CmdAdd(healAmount);
        }
    }
}
