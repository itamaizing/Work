using System;

public class MinionComponent : Character
{
    private HeroComponent _myHeroParent;

    public event Action<MinionComponent> Destroyed;

    public override void Initialize()
    {
        base.Initialize();
    }

    private void OnDestroy()
    {
        Destroyed?.Invoke(this);
    }
}
