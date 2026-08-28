using System;

public class TestMatchRequirementsChecker : IMatchRequirementsChecker
{
    public void CheckRequirements(Action<bool> onResult)
    {
        onResult?.Invoke(true);
    }
}