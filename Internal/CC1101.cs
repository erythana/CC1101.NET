using System.Device.Gpio;
using System.Diagnostics;
using CC1101.NET.Enums;
using CC1101.NET.Interfaces;
using CC1101.NET.Internal.Enums;

namespace CC1101.NET.Internal
{
    internal class CC1101 : ICC1101
    {
        #region member fields

        private ConnectionConfiguration _connectionConfiguration;
        private GpioController _gpioController;
        private RxTxStateController _rxTxStateController;
        private SPICommunication _spiCommunication;
        private byte _transmissionAddress;
        private int _channel;
        private Mode _mode;
        private Frequency _frequency;
        private bool _alreadyDisposed;

        #endregion

        #region constructor

        private CC1101(ConnectionConfiguration connectionConfiguration, IWakeOnRadio wakeOnRadio, GpioController gpioController, IPowerstate powerstate, RxTxStateController rxTxStateController,
            SPICommunication spiCommunication)
        {
            _connectionConfiguration = connectionConfiguration;
            _gpioController = gpioController;
            _rxTxStateController = rxTxStateController;
            _spiCommunication = spiCommunication;
            Powerstate = powerstate;
            WakeOnRadio = wakeOnRadio;
        }

        public static ICC1101 Create(ConnectionConfiguration connectionConfiguration, IWakeOnRadio wakeOnRadio, GpioController gpioController, IPowerstate powerstate, RxTxStateController rxTxStateController,
            SPICommunication spiCommunication)
        {
            var cc1101 = new CC1101(connectionConfiguration, wakeOnRadio, gpioController, powerstate, rxTxStateController, spiCommunication);
            cc1101.Initialize();

            return cc1101;
        }

        public async static Task<ICC1101> CreateAsync(ConnectionConfiguration connectionConfiguration, IWakeOnRadio wakeOnRadio, GpioController gpioController, IPowerstate powerstate,
            RxTxStateController rxTxStateController, SPICommunication spiCommunication, CancellationToken cancellationToken)
        {
            var cc1101 = new CC1101(connectionConfiguration, wakeOnRadio, gpioController, powerstate, rxTxStateController, spiCommunication);
            await cc1101.InitializeAsync(cancellationToken);

            return cc1101;
        }


        private void Initialize()
        {
            OpenGpioPins(_gpioController, _connectionConfiguration);

            Powerstate.Reset();
            _spiCommunication.WriteStrobe(CommandStrobe.SFTX);
            Thread.Sleep(50); // 
            _spiCommunication.WriteStrobe(CommandStrobe.SFRX);
            Thread.Sleep(50);

            Mode = Mode.GFSK__100kb;
            Frequency = Frequency.Frequency868;
            Channel = 0;
            Powerstate.SetOutputPower(OutputPower.MODERATE);
            DeviceAddress = 0x0F; //some 'random' initialization activateFEC
            _rxTxStateController.Receive();
        }

        private async Task InitializeAsync(CancellationToken cancellationToken)
        {
            OpenGpioPins(_gpioController, _connectionConfiguration);

            Powerstate.Reset();
            _spiCommunication.WriteStrobe(CommandStrobe.SFTX);
            await Task.Delay(50, cancellationToken);
            _spiCommunication.WriteStrobe(CommandStrobe.SFRX);
            await Task.Delay(50, cancellationToken);

            Mode = Mode.GFSK__100kb;
            Frequency = Frequency.Frequency868;
            Channel = 0;
            Powerstate.SetOutputPower(OutputPower.MODERATE);
            DeviceAddress = 0x0F; //some 'random' initialization activateFEC
            _rxTxStateController.Receive();
        }

        #endregion

        #region Properties

        public byte DeviceAddress {
            get => _transmissionAddress;
            set
            {
                _transmissionAddress = value;
                _spiCommunication.WriteRegister((byte)DeviceRegister.ADDR, _transmissionAddress);
            }
        }

        public int Channel {
            get => _channel;
            set
            {
                _channel = value;
                _spiCommunication.WriteRegister((byte)DeviceRegister.CHANNR, (byte)_channel);
            }
        }

        /// <summary>
        /// Gets or sets the modulation mode with all relevant settings
        /// </summary>
        public Mode Mode {
            get => _mode;
            set
            {
                _mode = value;
                var valuesToWrite = GetModeSettings(value);
                _spiCommunication.WriteBurst((byte)ReadWriteOffset.WriteBurst, valuesToWrite);
            }
        }

        public Frequency Frequency {
            get => _frequency;
            set
            {
                _frequency = value;
                var frequencies = GetFrequencySettings(value);
                _spiCommunication.WriteRegister((byte)DeviceRegister.FREQ2, (byte)frequencies.Frequency2);
                _spiCommunication.WriteRegister((byte)DeviceRegister.FREQ1, (byte)frequencies.Frequency1);
                _spiCommunication.WriteRegister((byte)DeviceRegister.FREQ0, (byte)frequencies.Frequency0);
            }
        }

        public IPowerstate Powerstate { get; private set; }

        public IWakeOnRadio WakeOnRadio { get; private set; }

        public void SendAcknowledge(byte receiverAddress)
        {
            var ackPayload = new[]
            {
                (byte)'A', (byte)'c', (byte)'k',
            };
            TxPayloadBurst(receiverAddress, ackPayload); // Load payload to CC1100
            _rxTxStateController.Transmit(); // Send packet over the air
            _rxTxStateController.Receive(); // Set CC1100 in receive mode
        }

        public bool CheckAcknowledge(byte receiverAddress, byte[] potentialAckPayload, out RFPacket? packet)
        {
            packet = null;

            if (potentialAckPayload.Length == 0x05 &&
                (potentialAckPayload[1] == DeviceAddress || potentialAckPayload[1] == (byte)DeviceRegister.BROADCAST_ADDRESS) &&
                potentialAckPayload[2] == receiverAddress &&
                potentialAckPayload[3] == 'A' && potentialAckPayload[4] == 'c' && potentialAckPayload[5] == 'k')
            {
                if (potentialAckPayload[1] == (byte)DeviceRegister.BROADCAST_ADDRESS)
                {
                    return false;
                }

                var packetLength = potentialAckPayload[0];
                var rssiDbm = ReceiverStrengthToDBM(potentialAckPayload[packetLength + 1]);
                var lqi = LqiConvert((sbyte)potentialAckPayload[packetLength + 2]);
                var crc = CheckCRC(lqi);

                packet = new RFPacket(receiverAddress, rssiDbm, lqi, crc, potentialAckPayload);
                return true;
            }

            return false;
        }

        public bool PacketAvailable()
        {
            if (_gpioController.Read(_connectionConfiguration.GDO2) != PinValue.High) //if RF packet received
                return false;

            if (_spiCommunication.ReadRegister((byte)DeviceRegister.IOCFG2) != 0x06) //if sync word detect mode is used
                return true;

            while (_gpioController.Read(_connectionConfiguration.GDO2) == PinValue.High)
            {
                //wait till sync word is fully received
            }

            return true;
        }

        /// <summary>
        /// Listens for packages for the specified time (in milliseconds)
        /// </summary>
        /// <param name="milliseconds">The time to listen for a package to receive</param>
        /// <returns></returns>
        public bool WaitForPacket(int milliseconds)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < milliseconds)
                if (PacketAvailable())
                    return true;
            return false;
        }

        /// <summary>
        /// Listens for packages for the specified time (in milliseconds)
        /// </summary>
        /// <param name="milliseconds">The time to listen for a package to receive</param>
        /// <param name="cancellationToken">The cancellationToken to cancel the Task before the time elapses</param>
        /// <returns></returns>
        public async Task<bool> WaitForPacketAsync(int milliseconds, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < milliseconds)
            {
                if (PacketAvailable())
                    return true;

                await Task.Delay(5, cancellationToken);
            }

            return false;
        }

        public void SetPowerAmplifierTable(byte[] powerAmplifierTable) =>
            _spiCommunication.WriteBurst((byte)DeviceRegister.PATABLE_BURST, powerAmplifierTable);

        public void SetPreambleLength(int length)
        {
            var data = _spiCommunication.ReadRegister((byte)DeviceRegister.MDMCFG1);
            data = (byte)(data & 0x8F |
                          length << 4 & 0x70); //TODO: Rework with Match.Clamp or whatever - So this code length << 4 & 0x70 performs a shift first, and then applies a mask to extract certain bits (4th to 6th bits). After these operations, length should fall in the range of 0 - 112 occupying binary bit positions 4-6.
            _spiCommunication.WriteRegister((byte)DeviceRegister.MDMCFG1, data);
        }

        /// <summary>
        /// Sets the baudrate, the correct crystal frequency is required
        /// Requires also that the correct deviation is set via SetDeviation(deviation, crystal)
        /// </summary>
        /// <param name="baudRate">The Baud-Rate - must be between 600 and 500000</param>
        /// <param name="crystalFrequency">Crystal frequency in Hz</param>
        public void SetDatarate(int baudRate, int crystalFrequency)
        {
            if (baudRate < 600 || baudRate > 500000)
                return;

            var mdmcfg4RegisterValue = _spiCommunication.ReadRegister((byte)DeviceRegister.MDMCFG4);
            var rbw = (byte)(mdmcfg4RegisterValue & 0b11110000);

            var dataRateExponent = (byte)Math.Log(baudRate * Math.Pow(2, 20) / crystalFrequency, 2); // log2 calculation
            var dataRateMantissa = (byte)(baudRate * Math.Pow(2, 28) / (crystalFrequency * Math.Pow(2, dataRateExponent)) - 256);
            mdmcfg4RegisterValue = (byte)(rbw | dataRateExponent & 0b00001111); // combining rbw and dataRateExponent

            _spiCommunication.WriteRegister((byte)DeviceRegister.MDMCFG4, mdmcfg4RegisterValue);
            _spiCommunication.WriteRegister((byte)DeviceRegister.MDMCFG3, dataRateMantissa);
        }

        /// <summary>
        /// Sets the deviation
        /// </summary>
        /// <param name="deviationHz">The deviation in Hz</param>
        /// <param name="crystalFrequency">Crystal frequency in Hz</param>
        public void SetDeviation(int deviationHz, int crystalFrequency)
        {
            var deviationExponent = 0;
            var deviationMantissa = 0;
            var closest = int.MaxValue;
            //calculate the best fitting/closest deviation for the 
            for (byte trialDeviationExponent = 0; trialDeviationExponent < 8; trialDeviationExponent++)
            {
                for (byte trialDeviationMantissa = 0; trialDeviationMantissa < 8; trialDeviationMantissa++)
                {
                    var calculatedDeviation = crystalFrequency / Math.Pow(2, 17) * (8 + trialDeviationMantissa) * Math.Pow(2, trialDeviationExponent);
                    var diff = Math.Abs(deviationHz - (int)calculatedDeviation);
                    if (diff >= closest)
                        continue;

                    closest = diff;
                    deviationMantissa = trialDeviationMantissa;
                    deviationExponent = trialDeviationExponent;
                }
            }

            var newDeviation = (byte)(deviationExponent << 4 | deviationMantissa);
            _spiCommunication.WriteRegister((byte)DeviceRegister.DEVIATN, newDeviation);
        }

        public void SetSyncMode(SyncMode syncMode)
        {
            var data = _spiCommunication.ReadRegister((byte)DeviceRegister.MDMCFG2);
            data = (byte)(data & 0xF8 | (byte)syncMode & 0x07);
            _spiCommunication.WriteRegister((byte)DeviceRegister.MDMCFG2, data);
        }

        public void SetFEC(bool activateFEC)
        {
            var data = _spiCommunication.ReadRegister((byte)DeviceRegister.MDMCFG1);
            var cfgByte = (byte)(activateFEC
                ? 1
                : 0);
            data = (byte)(data & 0x7F | (byte)(cfgByte << 7));
            _spiCommunication.WriteRegister((byte)DeviceRegister.MDMCFG1, data);
        }

        public void SetDataWhitening(bool activateWhitening)
        {
            var data = _spiCommunication.ReadRegister((byte)DeviceRegister.PKTCTRL0);
            var cfgByte = (byte)(activateWhitening
                ? 1
                : 0);
            data = (byte)(data & 0xBF | cfgByte << 6 & 0x40);
            _spiCommunication.WriteRegister((byte)DeviceRegister.PKTCTRL0, data);
        }

        public void SetManchesterEncoding(bool activateManchesterEncoding)
        {
            var data = _spiCommunication.ReadRegister((byte)DeviceRegister.MDMCFG2);
            var cfgByte = (byte)(activateManchesterEncoding
                ? 1
                : 0);
            data = (byte)(data & 0xF7 | cfgByte << 3 & 0x08);
            _spiCommunication.WriteRegister((byte)DeviceRegister.MDMCFG2, data);
        }

        /// <summary>
        /// Converts receiver strength to dBm
        /// </summary>
        /// <param name="rssiHex"></param>
        /// <returns></returns>
        public int ReceiverStrengthToDBM(byte rssiHex)
        {
            int dbmResult;
            short rssiDec = rssiHex; // Convert unsigned to signed;

            if (rssiDec >= 128)
                dbmResult = (rssiDec - 256) / 2 - (int)DeviceRegister.RSSI_OFFSET_868MHZ;
            else
                dbmResult = rssiDec / 2 - (int)DeviceRegister.RSSI_OFFSET_868MHZ;

            return dbmResult;
        }

        /// <summary>
        /// Get RF Quality Indicator
        /// The Link Quality Indicator is a metric of the current quality of the received signal.
        /// </summary>
        /// <param name="qualityIndicatorHex"></param>
        /// <returns></returns>
        public byte LqiConvert(sbyte qualityIndicatorHex) => (byte)(qualityIndicatorHex & 0x7F);

        /// <summary>
        /// Get Payload CRC
        /// </summary>
        /// <param name="checksumHex"></param>
        /// <returns></returns>
        public byte CheckCRC(byte checksumHex) => (byte)(checksumHex & 0x80);

        public void SetModulationType(ModulationType modulationType)
        {
            var data = _spiCommunication.ReadRegister((byte)DeviceRegister.MDMCFG2);
            data = (byte)(data & 0x8F | (byte)modulationType << 4 & 0x70);
            _spiCommunication.WriteRegister((byte)DeviceRegister.MDMCFG2, data);
        }

        /// <summary>
        /// Tries to send a payload to the specified receiver
        /// </summary>
        /// <param name="receiver">The receiver address. 0x00 to broadcast</param>
        /// <param name="transmitPayload">The payload to transmit</param>
        /// <param name="txRetries">The number of retries</param>
        /// <param name="ackPacket">When successfully sent the packet, the receiver might return an ACK</param>
        /// <returns></returns>
        public CommunicationResult<RFPacket> SendPacket(byte receiver, byte[] transmitPayload, int txRetries)
        {
            var result = new CommunicationResult<RFPacket>();
            var txRetriesCount = 0;

            if (transmitPayload.Length > (int)DeviceRegister.FIFOBUFFER - 1)
                return result;

            do //sent packet out with retries
            {
                TxPayloadBurst(receiver, transmitPayload);

                _rxTxStateController.Transmit();
                _rxTxStateController.Receive();

                if (receiver == (byte)DeviceRegister.BROADCAST_ADDRESS)
                {
                    //no wait acknowledge if sent to broadcast address or tx_retries = 0
                    result.Success = true;
                    return result; //successful sent to BROADCAST_ADDRESS, no ack packet
                }

                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < (int)DeviceRegister.ACK_TIMEOUT) //wait for an acknowledge
                {
                    if (PacketAvailable()) //if RF packet received check packet ack
                    {
                        var receiveBuffer = TryRxPayloadBurst(); //pktlen_ack returned        //reads packet in buffer
                        CheckAcknowledge(receiver, receiveBuffer.Value, out var packet); //check if received message is an acknowledge from client
                        result.Success = true;
                        result.Value = packet;
                    }

                    Thread.Sleep(5); //delay to give receiver time
                }

                txRetriesCount++; //increase tx retry counter
            } while (txRetriesCount <= txRetries); //while count of retries is reaches

            return result; //sent failed. too many retries
        }

        /// <summary>
        /// Tries to send a payload to the specified receiver
        /// </summary>
        /// <param name="receiver">The receiver address. 0x00 to broadcast</param>
        /// <param name="transmitPayload">The payload to transmit</param>
        /// <param name="txRetries">The number of retries</param>
        /// <param name="ackPacket">When successfully sent the packet, the receiver might return an ACK</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<CommunicationResult<RFPacket>> SendPacketAsync(byte receiver, byte[] transmitPayload, int txRetries, CancellationToken cancellationToken = default)
        {
            var result = new CommunicationResult<RFPacket>();

            if (transmitPayload.Length > (int)DeviceRegister.FIFOBUFFER - 1)
                return result;

            var txRetriesCount = 0;
            do //sent packet out with retries
            {
                TxPayloadBurst(receiver, transmitPayload);

                _rxTxStateController.Transmit();
                _rxTxStateController.Receive();

                if (receiver == (byte)DeviceRegister.BROADCAST_ADDRESS)
                {
                    //no wait acknowledge if sent to broadcast address or tx_retries = 0
                    result.Success = true;
                    return result;
                }

                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < (int)DeviceRegister.ACK_TIMEOUT) //wait for an acknowledge
                {
                    if (PacketAvailable()) //if RF packet received check packet ack
                    {
                        var receiveBuffer = await TryRxPayloadBurst(cancellationToken); //pktlen_ack returned        //reads packet in buffer
                        CheckAcknowledge(receiver, receiveBuffer.Value, out var packet); //check if received message is an acknowledge from client
                        result.Success = true;
                        result.Value = packet;
                        return result; //packet successfully sent
                    }

                    await Task.Delay(5, cancellationToken); //delay to give receiver time
                }

                txRetriesCount++; //increase tx retry counter
            } while (txRetriesCount <= txRetries); //while count of retries is reaches

            return result;
        }

        public void TxPayloadBurst(byte receiverAddress, byte[] transmitPayload)
        {
            var payload = new byte[transmitPayload.Length + 3];

            payload[0] = (byte)(payload.Length - 1);
            payload[1] = receiverAddress;
            payload[2] = DeviceAddress;
            Array.Copy(transmitPayload, 0, payload, 3, transmitPayload.Length);

            _spiCommunication.WriteBurst((byte)DeviceRegister.TXFIFO_BURST, transmitPayload); //writes TX_Buffer +1 because of pktlen must be also transfered
        }

        public CommunicationResult<byte[]> TryRxPayloadBurst()
        {
            var result = new CommunicationResult<byte[]>();
            var rxFifoByteCount = _spiCommunication.ReadRegister((byte)DeviceRegister.RXBYTES); //reads the number of bytes in RXFIFO

            if ((rxFifoByteCount & 0x7F) != 0 && (rxFifoByteCount & 0x80) == 0) //if bytes in buffer and no RX Overflow
            {
                var buffer = _spiCommunication.ReadBurst((byte)DeviceRegister.RXFIFO_BURST, rxFifoByteCount);
                result.Success = true;
                result.Value = buffer;
                return result;
            }

            Powerstate.Idle();
            _spiCommunication.WriteStrobe(CommandStrobe.SFRX);
            Thread.Sleep(50);
            _rxTxStateController.Receive();
            return result;
        }

        public async Task<CommunicationResult<byte[]>> TryRxPayloadBurst(CancellationToken cancellationToken = default)
        {
            var result = new CommunicationResult<byte[]>();

            var buffer = Array.Empty<byte>();
            var rxFifoByteCount = _spiCommunication.ReadRegister((byte)DeviceRegister.RXBYTES); //reads the number of bytes in RXFIFO

            if ((rxFifoByteCount & 0x7F) != 0 && (rxFifoByteCount & 0x80) == 0) //if bytes in buffer and no RX Overflow
            {
                buffer = _spiCommunication.ReadBurst((byte)DeviceRegister.RXFIFO_BURST, rxFifoByteCount);
                result.Success = true;
                result.Value = buffer;
                return result;
            }

            Powerstate.Idle();
            _spiCommunication.WriteStrobe(CommandStrobe.SFRX);
            await Task.Delay(50, cancellationToken);
            _rxTxStateController.Receive();
            return result;
        }

        public CommunicationResult<RFPacket> GetPayload()
        {
            var result = new CommunicationResult<RFPacket>();

            var rxPayloadBurst = TryRxPayloadBurst();

            if (!rxPayloadBurst.Success || CheckAcknowledge(rxPayloadBurst.Value[2], rxPayloadBurst.Value, out var _))
                return result;

            var receiver = rxPayloadBurst.Value[2];

            var packetLength = rxPayloadBurst.Value[0];
            var rssiDbm = ReceiverStrengthToDBM(rxPayloadBurst.Value[packetLength + 1]);
            var lqi = LqiConvert((sbyte)rxPayloadBurst.Value[packetLength + 2]);
            var crc = CheckCRC(lqi);
            result.Success = true;
            result.Value = new RFPacket(receiver, rssiDbm, lqi, crc, rxPayloadBurst.Value);
            if (DeviceAddress != (byte)DeviceRegister.BROADCAST_ADDRESS)
                SendAcknowledge(receiver);

            return result;
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            if (_alreadyDisposed)
                return;

            Powerstate.PowerDown();
            _spiCommunication.Dispose();
            _gpioController.Dispose();

            _alreadyDisposed = true;
        }

        #endregion

        #region helper methods

        private static byte[] GetModeSettings(Mode value) => value switch
        {
            Mode.GFSK__1_2kb => ModeConfiguration.CC1100_GFSK__1_2kb,
            Mode.GFSK__38_4kb => ModeConfiguration.CC1100_GFSK__38_4kb,
            Mode.GFSK__100kb => ModeConfiguration.CC1100_GFSK__100kb,
            Mode.MSK__250kb => ModeConfiguration.CC1100_MSK__250kb,
            Mode.MSK__500kb => ModeConfiguration.CC1100_MSK__500kb,
            Mode.OOK__4_8kb => ModeConfiguration.CC1100_OOK__4_8kb,
            _ => throw new NotImplementedException("The selected mode has not yet been implemented!")
        };

        private static (int Frequency0, int Frequency1, int Frequency2) GetFrequencySettings(Frequency value) => value switch
        {

            Frequency.Frequency315 => (0x89, 0x1D, 0x0C),
            Frequency.Frequency433 => (0x71, 0xB0, 0x10),
            Frequency.Frequency868 => (0x6A, 0x65, 0x21),
            Frequency.Frequency915 => (0x3B, 0x31, 0x23),
            _ => throw new NotImplementedException("The selected mode has not yet been implemented!")
        };

        private static void OpenGpioPins(GpioController gpioController, ConnectionConfiguration connectionConfiguration)
        {
            if (!gpioController.IsPinOpen(connectionConfiguration.GDO2))
                gpioController.OpenPin(connectionConfiguration.GDO2, PinMode.Input);
            if (!gpioController.IsPinOpen(connectionConfiguration.CS))
                gpioController.OpenPin(connectionConfiguration.CS, PinMode.Output);
        }

        #endregion
    }

}