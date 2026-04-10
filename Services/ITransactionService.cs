using WealthTracker.Models;
using WealthTracker.Requests;
using WealthTracker.Responses;

namespace WealthTracker.Services;

public interface ITransactionService
{ 
    Task<IEnumerable<TransactionResponseDto>> GetAllAsync(long userId, CancellationToken cancellationToken = default);
    Task<Transaction?> CreateAsync(long userId,TransactionCreateDto transaction, CancellationToken cancellationToken = default);
    Task<Transaction?> UpdateAsync(long userId,TransactionUpdateDto transaction, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long userId,long id, CancellationToken cancellationToken = default);

    Task<IEnumerable<TransactionResponseDto>> Filter(long userId, TransactionFilterDto filter, CancellationToken cancellationToken = default);
    Task<DashboardDataDto> GetCategorySummary(long userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MonthlySummaryDto>> GetMonthlySummary(long userId, CancellationToken cancellationToken = default);


}