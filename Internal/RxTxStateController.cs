using CC1101.NET.Enums;
using CC1101.NET.Interfaces;
using CC1101.NET.Internal.Enums;

namespace CC1101.NET.Internal;

internal class RxTxStateController
{
    private readonly SPICommunication _spiCommunication;

    public RxTxStateController(SPICommunication spiCommunication)
    {
        _spiCommunication = spiCommunication;
    }

    public void Receive()
    {
        _spiCommunication.WriteStrobe(CommandStrobe.SRX);
        var radioControlState = 0xff;
        while (radioControlState != 0x0D)
        {
            radioControlState = _spiCommunication.ReadRegister((byte)DeviceRegister.MARCSTATE & 0x1F);
        }
    }

    public void Transmit()
    {
        _spiCommunication.WriteStrobe(CommandStrobe.STX);
        var radioControlState = 0xff;
        while (radioControlState != 0x01)
        {
            radioControlState = _spiCommunication.ReadRegister((byte)DeviceRegister.MARCSTATE & 0x1F);
        }
    }
}