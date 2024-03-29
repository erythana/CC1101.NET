﻿using System.Device.Gpio;
using CC1101.NET.Interfaces;
using System.Device.Spi;
using CC1101.NET.Internal;

namespace CC1101.NET
{
    public sealed class CC1101Controller : ICC1101Init
    {
        #region member fields

        private ICC1101 _cc1101;
        private readonly ConnectionConfiguration _configuration;
        private readonly GpioController _gpioController;
        private readonly SPICommunication _spiCommunication;
        private readonly RxTxStateController _rxTxStateController;
        private readonly IPowerstate _powerstate;
        private IWakeOnRadio _wakeOnRadio;

        #endregion

        #region constructor

        /// <summary>
        /// Creates a new CC1101Controller-SPI Communication
        /// </summary>
        /// <param name="configuration">Optional - configure custom SPI-Pins</param>
        public CC1101Controller(ConnectionConfiguration? configuration)
        {
            _configuration = configuration ?? new ConnectionConfiguration();
            
            _gpioController = new GpioController(PinNumberingScheme.Logical);
            var spi = SpiDevice.Create(new SpiConnectionSettings(_configuration.Bus, _configuration.CS));
            _spiCommunication = new SPICommunication(spi);
            _rxTxStateController = new RxTxStateController(_spiCommunication);
            
            var powerstateController = new PowerstateController(_configuration, _gpioController, _spiCommunication);
            _powerstate = new Powerstate(powerstateController);

            _wakeOnRadio = new WakeOnRadio(_powerstate, _spiCommunication);
        }

        #endregion
        #region ICC01Init implementation
        
        /// <summary>
        /// Creates the CC1101 Device - Disposable!
        /// </summary>
        /// <param name="transmitterAddress">The address of this device. This can be changed later</param>
        /// <returns></returns>
        public ICC1101 Initialize(byte transmitterAddress)
        {
            _cc1101 = Internal.CC1101.Create(_configuration, _wakeOnRadio, _gpioController, _powerstate, _rxTxStateController, _spiCommunication);
            _cc1101.DeviceAddress = transmitterAddress;
            return _cc1101;
        }

        /// <summary>
        /// Creates the CC1101 Device - Disposable!
        /// </summary>
        /// <param name="transmitterAddress">The address of this device. This can be changed later</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ICC1101> InitializeAsync(byte transmitterAddress, CancellationToken cancellationToken)
        {
            _cc1101 = await Internal.CC1101.CreateAsync(_configuration, _wakeOnRadio, _gpioController, _powerstate, _rxTxStateController, _spiCommunication, cancellationToken);
            _cc1101.DeviceAddress = transmitterAddress;
            return _cc1101;
        }

        #endregion
    }
}