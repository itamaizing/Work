public interface IPolaritySwitchable
{
    bool IsLightMode { get; }
    void SwitchMode();
}