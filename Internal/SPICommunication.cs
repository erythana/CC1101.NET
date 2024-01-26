using System.Device.Spi;
using CC1101.NET.Enums;
using CC1101.NET.Internal.Enums;

namespace CC1101.NET.Internal;

internal class SPICommunication
{
    private readonly SpiDevice _spiDevice;
    private bool _alreadyDisposed;
    #region constructor
    
    public SPICommunication(SpiDevice spiDevice)
    {
        _spiDevice = spiDevice;
    }
    
    #endregion
    
    public void WriteStrobe(CommandStrobe commandStrobe) => 
        _spiDevice.Write(new []{(byte)commandStrobe});

    public void WriteRegister(byte spiInstruction, byte value)
    {
        var writeBuffer = new [] { (byte)(spiInstruction | (byte)ReadWriteOffset.WriteSingleByte), value };
        _spiDevice.Write(writeBuffer);
    }

    public byte ReadRegister(byte spiInstruction)
    { 
        var readBuffer = new byte[2];
        var writeBuffer = new byte[] { (byte)(spiInstruction | (byte)ReadWriteOffset.ReadSingleByte), 0x00 };

        _spiDevice.TransferFullDuplex(writeBuffer, readBuffer);
        return readBuffer[1];
    }

    public byte[] ReadBurst(byte spiInstruction, int length)
    {
        var readBuffer = new byte[length + 1];
        readBuffer[0] = (byte)(spiInstruction | (byte)ReadWriteOffset.ReadBurst);

        _spiDevice.TransferFullDuplex(readBuffer, readBuffer);
        return readBuffer[1..];
    }

    public void WriteBurst(byte spiInstruction, byte[] valuesToWrite)
    {
        var writeBuffer = new byte[valuesToWrite.Length+1];
        writeBuffer[0] = (byte)(spiInstruction | (byte)ReadWriteOffset.WriteBurst);
 
        for (var i=1; i<=valuesToWrite.Length; i++)
            writeBuffer[i] = valuesToWrite[i-1];
        _spiDevice.Write(writeBuffer);
    }

    #region IDisposable implementation

    public void Dispose()
    {
        if (_alreadyDisposed)
            return;

        _spiDevice.Dispose();

        _alreadyDisposed = true;
    }

    #endregion
}