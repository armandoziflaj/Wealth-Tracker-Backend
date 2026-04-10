namespace WealthTracker.Responses;

public class PaginatedResponse<T> : BaseResponse<List<T>>
{
    public long TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    
    public long TotalPages => PageSize > 0 
        ? (long)Math.Ceiling((double)TotalCount / PageSize) 
        : 0;

    public static PaginatedResponse<T> Success(List<T> data, long totalCount, int pageNumber, int pageSize)
    {
        return new PaginatedResponse<T>
        {
            IsSuccess = true,
            Data = data,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
    public new static PaginatedResponse<T> Failure(string error) => new()
    {
        IsSuccess = false,
        Errors = [error],
        Data = null
    };
}