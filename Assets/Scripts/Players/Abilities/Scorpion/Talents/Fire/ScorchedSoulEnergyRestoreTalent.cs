using UnityEngine;

public class ScorchedSoulEnergyRestoreTalent : Talent
{
    [SerializeField] private ScorchedSoulEnergyRestoreHandler _handler;

    public override void Enter()
    {
        _handler.Initialize(character);
        _handler.SetActive(true);
    }

    public override void Exit()
    {
        _handler.SetActive(false);
    }
}