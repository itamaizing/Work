using System;
using Mirror;

public class Level : NetworkBehaviour
{
    [SyncVar] protected int _experienceForNextLVL = 100;
    protected int _additionalToExperienceForNextLVL = 10;
    protected float _multiplierToExperienceForNextLVL = 1;
    private float _multiplierToExperience = 1;
    [SyncVar] private int _experience = 0;
    [SyncVar] private int _value = 1;

    public int Value { get => _value; protected set { _value = value; LVLUped?.Invoke(_value); } }
    public int Experience { get => _experience; }
    public int ExperienceForNextLVL { get => _experienceForNextLVL; }
    public float MultiplierToExperience { get => _multiplierToExperience; set => _multiplierToExperience = value; }

    public event Action<int> EXPAdded;
    public event Action<int> LVLUped;

    public void AddEXP(int value)
    {
        if (value <= 0)
            return;

        value = (int)(value * _multiplierToExperience);

        _experience += value;
        EXPAdded?.Invoke(value);

        var expBeyondNecessery = _experience - _experienceForNextLVL;

        if (expBeyondNecessery >= 0)
        {
            _value++;
            LVLUped?.Invoke(_value);

            _experience = 0;
            IncreasExperienceForNextLVL();
            AddEXP(expBeyondNecessery);
        }
    }

    private void IncreasExperienceForNextLVL()
    {
        _experienceForNextLVL = (int)(_experienceForNextLVL * _multiplierToExperienceForNextLVL) + _additionalToExperienceForNextLVL;
    }
}
