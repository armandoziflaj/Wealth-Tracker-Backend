using WealthTracker.Requests;

namespace WealthTracker.Services;

public interface IFileService
{
    Task<object> ParseExcelFile(IFormFile file, long userId, CancellationToken cancellationToken = default);
    Task<TransactionCreateDto> AiService(IFormFile file, long userId, CancellationToken cancellationToken = default);

}