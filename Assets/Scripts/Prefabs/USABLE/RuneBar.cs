public class RuneBar : Bar
{
    public override void UpdateValue(float hp, float maxHp)
    {
        _bar.value = hp/maxHp;
    }
}
