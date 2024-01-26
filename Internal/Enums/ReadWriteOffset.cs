namespace CC1101.NET.Internal.Enums;

[Flags]
internal enum ReadWriteOffset
{
    WriteSingleByte = 0x00,
    WriteBurst = 0x40,
    ReadSingleByte = 0x80,
    ReadBurst = 0xC0
}