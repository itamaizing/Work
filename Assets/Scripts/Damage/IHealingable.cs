using System;

public interface IHealingable
{
    public event Action<float, Skill> HealTaked;

    public void Heal(float value, Skill skill);
}
