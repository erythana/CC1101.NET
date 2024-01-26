using CC1101.NET.Enums;

namespace CC1101.NET.Interfaces;

public interface IPowerstate
{
    public void Reset();
    public void PowerDown();
    public void WakeUp();
    public void Idle();

    public void SetOutputPower(OutputPower dBm);
}