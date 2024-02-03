namespace CC1101.NET.Interfaces;

public interface ICC1101Init
{
    public ICC1101 Initialize(byte transmitterAddress);
    public Task<ICC1101> InitializeAsync(byte transmitterAddress, CancellationToken cancellationToken);
}