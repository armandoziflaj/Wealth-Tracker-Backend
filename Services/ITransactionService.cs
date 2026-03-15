using WealthTracker.Models;
using WealthTracker.Requests;
using WealthTracker.Responses;

namespace WealthTracker.Services;

public interface ITransactionService
{ 
    Task<IEnumerable<TransactionResponseDto>> GetAllAsync(long userId);
    Task<Transaction?> CreateAsync(long userId,Transaction transaction);
    Task<IEnumerable<TransactionResponseDto>> Filter(long userId, TransactionFilterDto filter);

}