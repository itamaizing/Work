public interface IHeroProgressPageValidator
{
    bool Validate(HeroComponent hero, HeroProgressSnapshot snapshot, out string error);
}