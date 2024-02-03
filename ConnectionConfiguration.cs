using CC1101.NET.Enums;

namespace CC1101.NET;

public sealed class ConnectionConfiguration
{
    #region constructor

    /// <summary>
    /// Creates the default ConnectionConfiguration on Bus0
    /// </summary>
    public ConnectionConfiguration() : this(BusConfiguration.Bus0) { }

    /// <summary>
    /// Creates the default ConnectionConfiguration for the selected BusConfiguration
    /// </summary>
    /// <param name="busConfigurationConfiguration">The selected Bus</param>
    /// <param name="gpioCommunicationPin">Any GPIO pin to communicate with the CC1101Controller device</param>
    /// <exception cref="NotImplementedException">Thrown, when the selected Bus has not yet been implemented</exception>
    public ConnectionConfiguration(BusConfiguration busConfigurationConfiguration, int gpioCommunicationPin = 22)
    {
        switch (busConfigurationConfiguration)
        {
            case BusConfiguration.Bus0:
                ConfigureForBus0(gpioCommunicationPin);
                break;
            case BusConfiguration.Bus1:
                ConfigureForBus1(gpioCommunicationPin);
                break;
            default:
                throw new NotImplementedException("This BusConfiguration has not yet been implemented!");
        }
    }
    
    /// <summary>
    /// Creates a ConnectionConfiguration, maps the CC101 Pins to the Raspberry PI - use the GPIO Pin numbers, not the physical ones!
    /// </summary>
    /// <param name="bus">The Bus ID of the SPI Device</param>
    /// <param name="si">The SI Pin</param>
    /// <param name="so">The SO Pin</param>
    /// <param name="cs">The CS Pin</param>
    /// <param name="sclk">The SCLK Pin</param>
    /// <param name="gdo2">Any GPIO pin to communicate with the CC1101Controller device</param>
    public ConnectionConfiguration(int bus = 0, int si = 10, int so = 9, int cs = 8, int sclk = 11, int gdo2 = 25)
    {
        Bus = bus;
        SI = si;
        SO = so;
        CS = cs;
        SCLK = sclk;
        GDO2 = gdo2;
    }

    #endregion

    #region properties

    public int Bus { get; private set; }

    public int SI { get; private set; }
    public int SO { get; private set; }
    public int CS { get; private set; }
    public int SCLK { get; private set; }
    public int GDO2 { get; private set; }

    #endregion

    #region helper methods

    private void ConfigureForBus0(int gpioCommunicationPin)
    {
        Bus = 0;
        SI = 10;
        SO = 9;
        CS = 8;
        SCLK = 11;
        GDO2 = gpioCommunicationPin;
    }
    
    private void ConfigureForBus1(int gpioCommunicationPin)
    {
        Bus = 1;
        SI = 20;
        SO = 19;
        CS = 18;
        SCLK = 21;
        GDO2 = gpioCommunicationPin;
    }

    #endregion
}