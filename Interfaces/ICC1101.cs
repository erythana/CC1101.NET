using CC1101.NET.Enums;

namespace CC1101.NET.Interfaces;

public interface ICC1101
{
    #region properties
    
    public byte DeviceAddress { get; set; }
    public Frequency Frequency { get; set; }
    public Mode Mode { get; set; }
    public int Channel { get; set; }
    public IPowerstate Powerstate { get; }    
    public IWakeOnRadio WakeOnRadio { get; }
    
    #endregion

    #region Methods


    public bool PacketAvailable();
    public void SendAcknowledge(byte receiverAddress);
    public bool CheckAcknowledge(byte receiverAddress, byte[] potentialAckPayload, out RFPacket? packet);
    public bool WaitForPacket(int milliseconds);
    public void SetPowerAmplifierTable(byte[] powerAmplifierTable);
    public void SetPreambleLength(int length);
    public void SetDatarate(byte mdmcfg4Value, byte mdmcfg3Value, byte deviation);
    public void SetSyncMode(SyncMode syncMode);
    public void SetFEC(bool activateFEC);
    public void SetDataWhitening(bool activateWhitening);
    public void SetManchesterEncoding(bool activateManchesterEncoding);
    public int ReceiverStrengthToDBM(byte rssiHex);
    public byte LqiConvert(sbyte qualityIndicatorHex);
    public byte CheckCRC(byte checksumHex);
    public void SetModulationType(ModulationType modulationType);
    public bool SendPacket(byte receiver, byte[] transmitPayload, int txRetries, out RFPacket? ackPacket);
    public void TxPayloadBurst(byte receiverAddress, byte[] transmitPayload);
    public bool TryRxPayloadBurst(out byte[] buffer);
    public bool GetPayload(out RFPacket? packet);

    #endregion

}