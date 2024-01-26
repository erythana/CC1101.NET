using System.Device.Gpio;
using CC1101.NET.Enums;
using CC1101.NET.Interfaces;
using CC1101.NET.Internal.Enums;

namespace CC1101.NET.Internal;

internal class Powerstate : IPowerstate
{
    private readonly PowerstateController _powerstateController;

    public Powerstate(PowerstateController powerstateController)
    {
        _powerstateController = powerstateController;
        
    }
    
    //TODO: Make async?!
    public void Reset()
    {
        _powerstateController.GpioController.Write(_powerstateController.ConnectionConfiguration.CS, PinValue.Low);
        Thread.Sleep(10);
        _powerstateController.GpioController.Write(_powerstateController.ConnectionConfiguration.CS, PinValue.High);
        Thread.Sleep(50);

        _powerstateController.SpiCommunication.WriteStrobe(CommandStrobe.SRES);
        
        Thread.Sleep(5);
    }

    public void PowerDown()
    {
        Idle();
        _powerstateController.SpiCommunication.WriteStrobe(CommandStrobe.SPWD);
    }

    public void WakeUp()
    {
        _powerstateController.GpioController.Write(_powerstateController.ConnectionConfiguration.CS, PinValue.Low);
        Thread.Sleep(10);
        _powerstateController.GpioController.Write(_powerstateController.ConnectionConfiguration.CS, PinValue.High);
        Thread.Sleep(50);
        
        _powerstateController.RxTxStateController.Receive();
    }

    public void Idle()
    {
        _powerstateController.SpiCommunication.WriteStrobe(CommandStrobe.SIDLE);
        var radioControlState = 0xff;
        while (radioControlState != 0x01)
        {
            radioControlState = _powerstateController.SpiCommunication.ReadRegister((byte)DeviceRegister.MARCSTATE & 0x1F);
        }

        Thread.Sleep(100);
    }

    public void SetOutputPower(OutputPower dBm) => 
        _powerstateController.SpiCommunication.WriteRegister((int)DeviceRegister.FREND0, (byte)dBm);
}