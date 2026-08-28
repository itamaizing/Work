public static class MatchRequirementsService
{
    public static IMatchRequirementsChecker Current { get; set; } = new TestMatchRequirementsChecker();
}