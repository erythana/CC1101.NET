using System.Device.Gpio;
using System.Diagnostics;
using CC1101.NET.Enums;
using CC1101.NET.Interfaces;
using CC1101.NET.Internal.Enums;

namespace CC1101.NET.Internal
{
    internal class CC1101 : ICC1101, IDisposable //Todo: CC1101 for dispose or from controller?
    {
        #region member fields
        
        private readonly ConnectionConfiguration _connectionConfiguration;
        private readonly GpioController _gpioController;
        private readonly RxTxStateController _rxTxStateController;
        private readonly SPICommunication _spiCommunication;
        private byte _transmissionAddress;
        private int _channel;
        private Mode _mode;
        private Frequency _frequency;
        private bool _alreadyDisposed;
        
        #endregion

        #region constructor

        internal CC1101(ConnectionConfiguration connectionConfiguration, IWakeOnRadio wakeOnRadio, GpioController gpioController, IPowerstate powerstate, RxTxStateController rxTxStateController,
            SPICommunication spiCommunication)
        {
            while (!Debugger.IsAttached)
            {
                
            }
            
            
            _connectionConfiguration = connectionConfiguration;
            _gpioController = gpioController;
            _rxTxStateController = rxTxStateController;
            _spiCommunication = spiCommunication;
            Powerstate = powerstate;
            WakeOnRadio = wakeOnRadio;

            Initialize();
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

        public IPowerstate Powerstate { get; }

        public IWakeOnRadio WakeOnRadio { get; }

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
                var rssiDbm = ReceiverStrengthToDBM(potentialAckPayload[packetLength+1]);
                var lqi = LqiConvert((sbyte)potentialAckPayload[packetLength+2]);
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

        public bool WaitForPacket(int milliseconds)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < milliseconds)
                if (PacketAvailable())
                    return true;
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

        public void SetDatarate(byte mdmcfg4Value, byte mdmcfg3Value, byte deviation)
        {
            _spiCommunication.WriteRegister((byte)DeviceRegister.MDMCFG4, mdmcfg4Value);
            _spiCommunication.WriteRegister((byte)DeviceRegister.MDMCFG3, mdmcfg3Value);
            _spiCommunication.WriteRegister((byte)DeviceRegister.DEVIATN, deviation);
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

        public bool SendPacket(byte receiver, byte[] transmitPayload, int txRetries, out RFPacket? ackPacket)
        {
            ackPacket = null;
            var txRetriesCount = 0;

            if (transmitPayload.Length > (int)DeviceRegister.FIFOBUFFER - 1)
            {
                //printf("ERROR: packet size overflow\r\n");
                return false;
            }

            do //sent packet out with retries
            {
                TxPayloadBurst(receiver, transmitPayload);

                _rxTxStateController.Transmit();
                _rxTxStateController.Receive();

                if (receiver == (byte)DeviceRegister.BROADCAST_ADDRESS)
                {
                    //no wait acknowledge if sent to broadcast address or tx_retries = 0
                    return true; //successful sent to BROADCAST_ADDRESS
                }

                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < (int)DeviceRegister.ACK_TIMEOUT) //wait for an acknowledge
                {
                    if (PacketAvailable()) //if RF packet received check packet ack
                    {
                        TryRxPayloadBurst(out var receiveBuffer); //pktlen_ack returned        //reads packet in buffer
                        CheckAcknowledge(receiver, receiveBuffer, out var packet); //check if received message is an acknowledge from client
                        ackPacket = packet;
                        return true; //packet successfully sent
                    }

                    Thread.Sleep(1); //delay to give receiver time
                }

                txRetriesCount++; //increase tx retry counter
            } while (txRetriesCount <= txRetries); //while count of retries is reaches

            return false; //sent failed. too many retries
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

        public bool TryRxPayloadBurst(out byte[] buffer)
        {
            buffer = Array.Empty<byte>();
            var rxFifoByteCount = _spiCommunication.ReadRegister((byte)DeviceRegister.RXBYTES); //reads the number of bytes in RXFIFO

            if ((rxFifoByteCount & 0x7F) != 0 && (rxFifoByteCount & 0x80) == 0) //if bytes in buffer and no RX Overflow
            {
                buffer = _spiCommunication.ReadBurst((byte)DeviceRegister.RXFIFO_BURST, rxFifoByteCount);
                return true;
            }
            
            Powerstate.Idle();
            _spiCommunication.WriteStrobe(CommandStrobe.SFRX);
            Thread.Sleep(50);
            _rxTxStateController.Receive();
            return false;
        }
        
        public bool GetPayload(out RFPacket? packet)
        {
            packet = null;
            
            if (TryRxPayloadBurst(out var rxBuffer) == false || CheckAcknowledge(rxBuffer[2], rxBuffer, out var _))
                return false;

            var receiver = rxBuffer[2];

            var packetLength = rxBuffer[0];
            var rssiDbm = ReceiverStrengthToDBM(rxBuffer[packetLength+1]);
            var lqi = LqiConvert((sbyte)rxBuffer[packetLength+2]);
            var crc = CheckCRC(lqi);
            packet = new RFPacket(receiver, rssiDbm, lqi, crc, rxBuffer);
            
            if (DeviceAddress != (byte)DeviceRegister.BROADCAST_ADDRESS)
                SendAcknowledge(receiver);

            return true;
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