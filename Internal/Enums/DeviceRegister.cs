namespace CC1101.NET.Internal.Enums;

internal enum DeviceRegister
{
    IOCFG2 = 0x00,      // GDO2 output pin configuration
    IOCFG1 = 0x01,      // GDO1 output pin configuration
    IOCFG0 = 0x02,      // GDO0 output pin configuration
    FIFOTHR = 0x03,     // RX FIFO and TX FIFO thresholds
    SYNC1 = 0x04,       // Sync word, high byte
    SYNC0 = 0x05,       // Sync word, low byte
    PKTLEN = 0x06,      // Packet length
    PKTCTRL1 = 0x07,    // Packet automation control
    PKTCTRL0 = 0x08,    // Packet automation control
    ADDR = 0x09,        // Device address
    CHANNR = 0x0A,      // Channel number
    FSCTRL1 = 0x0B,     // Frequency synthesizer control
    FSCTRL0 = 0x0C,     // Frequency synthesizer control
    FREQ2 = 0x0D,       // Frequency control word, high byte
    FREQ1 = 0x0E,       // Frequency control word, middle byte
    FREQ0 = 0x0F,       // Frequency control word, low byte
    MDMCFG4 = 0x10,     // Modem configuration
    MDMCFG3 = 0x11,     // Modem configuration
    MDMCFG2 = 0x12,     // Modem configuration
    MDMCFG1 = 0x13,     // Modem configuration
    MDMCFG0 = 0x14,     // Modem configuration
    DEVIATN = 0x15,     // Modem deviation setting
    MCSM2 = 0x16,       // Main Radio Cntrl State Machine config
    MCSM1 = 0x17,       // Main Radio Cntrl State Machine config
    MCSM0 = 0x18,       // Main Radio Cntrl State Machine config
    FOCCFG = 0x19,      // Frequency Offset Compensation config
    BSCFG = 0x1A,       // Bit Synchronization configuration
    AGCCTRL2 = 0x1B,    // AGC control
    AGCCTRL1 = 0x1C,    // AGC control
    AGCCTRL0 = 0x1D,    // AGC control
    WOREVT1 = 0x1E,     // High byte Event 0 timeout
    WOREVT0 = 0x1F,     // Low byte Event 0 timeout
    WORCTRL = 0x20,     // Wake On Radio control
    FREND1 = 0x21,      // Front end RX configuration
    FREND0 = 0x22,      // Front end TX configuration
    FSCAL3 = 0x23,      // Frequency synthesizer calibration
    FSCAL2 = 0x24,      // Frequency synthesizer calibration
    FSCAL1 = 0x25,      // Frequency synthesizer calibration
    FSCAL0 = 0x26,      // Frequency synthesizer calibration
    RCCTRL1 = 0x27,     // RC oscillator configuration
    RCCTRL0 = 0x28,     // RC oscillator configuration
    FSTEST = 0x29,      // Frequency synthesizer cal control
    PTEST = 0x2A,       // Production test
    AGCTEST = 0x2B,     // AGC test
    TEST2 = 0x2C,       // Various test settings
    TEST1 = 0x2D,       // Various test settings
    TEST0 = 0x2E,        // Various test settings
    
    PARTNUM = 0xF0,             // Part number
    VERSION = 0xF1,             // Current version number
    FREQEST = 0xF2,             // Frequency offset estimate
    LQI = 0xF3,                 // Demodulator estimate for link quality
    RSSI = 0xF4,                // Received signal strength indication
    MARCSTATE = 0xF5,           // Control state machine state
    WORTIME1 = 0xF6,            // High byte of WOR timer
    WORTIME0 = 0xF7,            // Low byte of WOR timer
    PKTSTATUS = 0xF8,           // Current GDOx status and packet status
    VCO_VC_DAC = 0xF9,          // Current setting from PLL cal module
    TXBYTES = 0xFA,             // Underflow and # of bytes in TXFIFO
    RXBYTES = 0xFB,             // Overflow and # of bytes in RXFIFO
    RCCTRL1_STATUS = 0xFC,      //Last RC Oscillator Calibration Result
    RCCTRL0_STATUS = 0xFD,       //Last RC Oscillator Calibration Result
    
    CFG_REGISTER = 0x2F,                //47 registers
    FIFOBUFFER = 0x42,                  //size of Fifo Buffer +2 for rssi and lqi
    RSSI_OFFSET_868MHZ = 0x4E,          //dec = 74
    TX_RETRIES_MAX = 0x05,              //tx_retries_max
    ACK_TIMEOUT = 250,                  //ACK timeout in ms
    CC1101_COMPARE_REGISTER = 0x00,     //register compare 0=no compare 1=compare
    BROADCAST_ADDRESS = 0x00,           //broadcast address
    CC1101_FREQ_315MHZ = 0x01,
    CC1101_FREQ_434MHZ = 0x02,
    CC1101_FREQ_868MHZ = 0x03,
    CC1101_FREQ_915MHZ = 0x04,
    
    TXFIFO_BURST = 0x7F,            //write burst only
    TXFIFO_SINGLE_BYTE = 0x3F,      //write single only
    RXFIFO_BURST = 0xFF,            //read burst only
    RXFIFO_SINGLE_BYTE = 0xBF,      //read single only
    PATABLE_BURST = 0x7E,           //power control read/write
    PATABLE_SINGLE_BYTE = 0xFE,     //power control read/write
}