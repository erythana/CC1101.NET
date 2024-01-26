namespace CC1101.NET.Internal.Enums;

internal enum CommandStrobe
{
    SRES = 0x30,        // Reset chip
    SFSTXON = 0x31,     // Enable/calibrate freq synthesizer
    SXOFF = 0x32,       // Turn off crystal oscillator.
    SCAL = 0x33,        // Calibrate freq synthesizer & disable
    SRX = 0x34,         // Enable RX.
    STX = 0x35,         // Enable TX.
    SIDLE = 0x36,       // Exit RX / TX
    SAFC = 0x37,        // AFC adjustment of freq synthesizer
    SWOR = 0x38,        // Start automatic RX polling sequence
    SPWD = 0x39,        // Enter pwr down mode when CSn goes hi
    SFRX = 0x3A,        // Flush the RX FIFO buffer.
    SFTX = 0x3B,        // Flush the TX FIFO buffer.
    SWORRST = 0x3C,     // Reset real time clock.
    SNOP = 0x3D         // No operation.
}