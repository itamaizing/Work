using Gangdollarff;
using UnityEngine;

public class FireworkBlindTalent : Talent
{
    [SerializeField] private FireworkDsplay _fireworkDsplay;
    public override void Enter()
    {
        _fireworkDsplay.SetBlinding(true);
    }

    public override void Exit()
    {
        _fireworkDsplay.SetBlinding(false);
    }
}
