namespace WealthTracker.Requests;

public class BaseRequest<T>
{
    public T? Data { get; set; }
}