using System;


public interface IMatchRequirementsChecker
{
    void CheckRequirements(Action<bool> onResult);
}