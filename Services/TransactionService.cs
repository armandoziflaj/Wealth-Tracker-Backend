using Microsoft.EntityFrameworkCore;
using WealthTracker.Models;
using WealthTracker.Requests;
using WealthTracker.Responses;

namespace WealthTracker.Services;

public class TransactionService(ApplicationDbContext context) : ITransactionService
{
    public async Task<IEnumerable<TransactionResponseDto>> GetAllAsync(long userId)
    {
        return await context.Transactions
                            .Include(t => t.Category)
                            .Where(t => !t.IsDeleted && t.UserId == userId)
                            .Select(t => new TransactionResponseDto
                            {
                                Id = t.Id,
                                Amount = t.Amount,
                                Description = t.Description,
                                TransactionDate = t.TransactionDate,
                                Type = (int)t.Type,
                                CategoryId = t.CategoryId,
                                CategoryName = t.Category.Name,
                                CategoryIcon = t.Category.Icon,
                                CategoryColor = t.Category.Color,
                                Notes = t.Notes,
                                IsRecurring = t.IsRecurring
                            })
                            .ToListAsync();
    }

    public async Task<Transaction?> CreateAsync(long userId, Transaction transaction)
    {
        transaction.UserId = userId;
        transaction.CreatedByUserId = userId.ToString();
        transaction.CreatedOn = DateTime.UtcNow;

        try
        {
            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();
            return transaction;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<IEnumerable<TransactionResponseDto>> Filter(long userId, TransactionFilterDto filter)
    {
        var query = context.Transactions.Where(t => t.UserId == userId);

        if (filter.FromDate.HasValue)
            query = query.Where(t => t.TransactionDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(t => t.TransactionDate <= filter.ToDate.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == filter.CategoryId.Value);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
            query = query.Where(t => t.Description.Contains(filter.SearchTerm)
                                     || (t.Notes != null && t.Notes.Contains(filter.SearchTerm)));

        return await query.OrderByDescending(t => t.TransactionDate)
                          .Select(t => new TransactionResponseDto
                          {
                            Id = t.Id,
                            Amount = t.Amount,
                            Description = t.Description,
                            TransactionDate = t.TransactionDate,
                            Type = (int)t.Type,
                            CategoryId = t.CategoryId,
                            CategoryName = t.Category.Name,
                            CategoryIcon = t.Category.Icon,
                            CategoryColor = t.Category.Color,
                            Notes = t.Notes,
                            IsRecurring = t.IsRecurring
                          })
                          .AsNoTracking()
                          .ToListAsync();
    }
}