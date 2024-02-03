using CC1101.NET.Enums;
using CC1101.NET.Internal;

namespace CC1101.NET.Interfaces;

public interface ICC1101 : IDisposable
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
    public void SetDatarate(int baudRate, int crystalFrequency);
    public void SetDeviation(int deviationHz, int crystalFrequency);
    public void SetSyncMode(SyncMode syncMode);
    public void SetFEC(bool activateFEC);
    public void SetDataWhitening(bool activateWhitening);
    public void SetManchesterEncoding(bool activateManchesterEncoding);
    public int ReceiverStrengthToDBM(byte rssiHex);
    public byte LqiConvert(sbyte qualityIndicatorHex);
    public byte CheckCRC(byte checksumHex);
    public void SetModulationType(ModulationType modulationType);
    public CommunicationResult<RFPacket> SendPacket(byte receiver, byte[] transmitPayload, int txRetries);
    public Task<CommunicationResult<RFPacket>> SendPacketAsync(byte receiver, byte[] transmitPayload, int txRetries, CancellationToken cancellationToken = default);
    public void TxPayloadBurst(byte receiverAddress, byte[] transmitPayload);
    public CommunicationResult<byte[]> TryRxPayloadBurst();
    public Task<CommunicationResult<byte[]>> TryRxPayloadBurst(CancellationToken cancellationToken = default);
    public CommunicationResult<RFPacket> GetPayload();

    #endregion

}