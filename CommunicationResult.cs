namespace CC1101.NET;

public class CommunicationResult<T>
{
    public bool Success { get; set; }
    public T? Value { get; set; }
}