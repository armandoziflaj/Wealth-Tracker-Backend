namespace WealthTracker.Services;

public interface IFileService
{
    Task<object> ParseExcelFile(IFormFile file, long userId, CancellationToken cancellationToken = default);

}