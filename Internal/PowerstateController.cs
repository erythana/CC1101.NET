using System.Device.Gpio;
using CC1101.NET.Interfaces;

namespace CC1101.NET.Internal;

internal class PowerstateController
{
    private readonly ConnectionConfiguration _connectionConfiguration;
    private readonly GpioController _gpioController;
    private readonly SPICommunication _spiCommunication;
    #region constructor

    public PowerstateController(ConnectionConfiguration connectionConfiguration, GpioController gpioController, SPICommunication spiCommunication)
    {
        ConnectionConfiguration = connectionConfiguration;
        GpioController = gpioController;
        SpiCommunication = spiCommunication;
        RxTxStateController = new RxTxStateController(SpiCommunication);
    }

    #endregion
    public ConnectionConfiguration ConnectionConfiguration { get; }
    public GpioController GpioController { get; }
    public SPICommunication SpiCommunication { get; }
    public RxTxStateController RxTxStateController { get; }
}