namespace CC1101.NET;

public sealed class RFPacket
{
    internal RFPacket(byte receiver, int dbm, byte lqi, byte crc, byte[] payload)
    {
        Receiver = receiver;
        DBM = dbm;
        Lqi = lqi;
        CRC = crc;
        Payload = payload;
    }

    public byte Receiver { get; }
    public int DBM { get; }
    public byte Lqi { get; }
    public byte CRC { get; }
    public byte[] Payload { get; }
}