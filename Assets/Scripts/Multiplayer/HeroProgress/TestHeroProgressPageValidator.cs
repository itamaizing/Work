public class TestHeroProgressPageValidator : IHeroProgressPageValidator
{
    public bool Validate(HeroComponent hero, HeroProgressSnapshot snapshot, out string error)
    {
        error = null;
        return true;
    }
}