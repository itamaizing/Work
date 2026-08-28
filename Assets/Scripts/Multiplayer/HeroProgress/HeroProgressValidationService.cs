public static class HeroProgressValidationService
{
    public static IHeroProgressPageValidator Current { get; set; } = new TestHeroProgressPageValidator();
}