namespace WealthTracker.Responses;

public class BaseResponse<T>
{
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = [];
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }

    public BaseResponse(T data, string? message = null)
    {
        IsSuccess = true;
        Data = data;
        Message = message;
    }

    public BaseResponse(List<string> errors)
    {
        IsSuccess = false;
        Errors = errors;
    }

    public BaseResponse() { } 
}