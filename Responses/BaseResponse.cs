namespace WealthTracker.Responses;

public class BaseResponse<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = [];

    public BaseResponse() { }

    public static BaseResponse<T> Success(T? data) => new()
    {
        IsSuccess = true,
        Data = data,
    };

    public static BaseResponse<T> Failure(string error) => new()
    {
        IsSuccess = false,
        Errors = [error],
    };

    public static BaseResponse<T> Failure(List<string> errors) => new()
    {
        IsSuccess = false,
        Errors = errors,
    };
    public static implicit operator BaseResponse<T>(T data) => Success(data);
    public static implicit operator BaseResponse<T>(string error) => Failure(error);
}