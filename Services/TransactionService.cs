using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WealthTracker.Models;
using WealthTracker.Requests;
using WealthTracker.Responses;

namespace WealthTracker.Services;

public class TransactionService(ApplicationDbContext context) : ITransactionService
{
    public async Task<IEnumerable<TransactionResponseDto>> GetAllAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await context.Transactions
                            .Include(t => t.Category)
                            .Where(t => t.UserId == userId)
                            .Select(t => new TransactionResponseDto
                            {
                                Id = t.Id,
                                Amount = t.Amount,
                                Description = t.Description,
                                TransactionDate = t.TransactionDate,
                                Type = (int)t.Type,
                                CategoryId = t.CategoryId,
                                CategoryName = t.Category.Name,
                                CategoryColor = t.Category.Color,
                                Notes = t.Notes,
                                IsRecurring = t.IsRecurring
                            })
                            .ToListAsync(cancellationToken);
    }

    public async Task<Transaction?> CreateAsync(long userId, TransactionCreateDto transaction, CancellationToken cancellationToken = default)
    {
        var newTransaction = new Transaction()
            {
                Amount = transaction.Amount,
                Description = transaction.Description,
                TransactionDate = DateTime.SpecifyKind(transaction.TransactionDate, DateTimeKind.Utc),      
                Type = transaction.Type,
                CategoryId = transaction.CategoryId,
                IsRecurring = transaction.IsRecurring,
                Notes = transaction.Notes,
                UserId = userId,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };
        try
        {
            context.Transactions.Add(newTransaction);
            await context.SaveChangesAsync(cancellationToken);
            return newTransaction;
        }
        catch (Exception)
        {
            return null;
        }
    }
    public async Task<Transaction?> UpdateAsync(long userId, TransactionUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var existingTransaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == dto.Id && t.UserId == userId, cancellationToken);

        if (existingTransaction == null) return null;

        existingTransaction.Amount = dto.Amount;
        existingTransaction.Description = dto.Description;
        existingTransaction.TransactionDate = DateTime.SpecifyKind(dto.TransactionDate, DateTimeKind.Utc);
        existingTransaction.Type = dto.Type;
        existingTransaction.CategoryId = dto.CategoryId;
        existingTransaction.Notes = dto.Notes;
        existingTransaction.IsRecurring = dto.IsRecurring;
        existingTransaction.UpdatedOn = DateTime.UtcNow; 

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return existingTransaction;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(long userId, long id, CancellationToken cancellationToken = default)
    {
        var existingTransaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

        if (existingTransaction == null) return false;
        
        try
        {
            context.Transactions.Remove(existingTransaction);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    public async Task<IEnumerable<TransactionResponseDto>> Filter(long userId, TransactionFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = context.Transactions.Where(t => t.UserId == userId);

        query = ApplyFilters(query, filter); 

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
                            CategoryColor = t.Category.Color,
                            Notes = t.Notes,
                            IsRecurring = t.IsRecurring
                          })
                          .AsNoTracking()
                          .ToListAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<MonthlySummaryDto>> GetMonthlySummary(long userId, CancellationToken cancellationToken = default)
    {
        var monthlyData = await context.Transactions
            .Where(t => t.UserId == userId && t.TransactionDate.Year == DateTime.Now.Year)
            .GroupBy(t => new { t.TransactionDate.Month, t.Type })
            .Select(g => new
            {
                g.Key.Month,
                g.Key.Type,
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);
        
        return Enumerable.Range(1, 12).Select(m => {
            var monthIncomes = monthlyData.Where(d => d.Month == m && d.Type == TransactionType.Income).Sum(d => d.Total);
            var monthExpenses = monthlyData.Where(d => d.Month == m && d.Type == TransactionType.Expense).Sum(d => d.Total);
        
            return new MonthlySummaryDto
            {
                Name = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m),
                Revenue = monthIncomes,
                Expenses = monthExpenses
            };
        }).ToList();

    }

    public async Task<DashboardDataDto> GetCategorySummary(
        long userId, 
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;

        var transactions = await context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && 
                        t.TransactionDate.Month == now.Month && 
                        t.TransactionDate.Year == now.Year)
            .ToListAsync(cancellationToken);

        var byType = transactions
            .GroupBy(t => t.Type)
            .Select(g => new CategorySummaryDto
            {
                Name = g.Key == TransactionType.Expense ? "Outflow" : "Inflow",
                Value = Math.Round(g.Sum(t => t.Amount), 2),
                Fill = g.Key == TransactionType.Expense ? "#FF4D4D" : "#C6FF5E"
            })
            .ToList();
        
        var byExpenseCategory = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => new { t.Category.Name, t.Category.Color })
            .Select(g => new CategorySummaryDto
            {
                Name = g.Key.Name,
                Value = Math.Round(g.Sum(t => t.Amount), 2),
                Fill = g.Key.Color ?? "#9CA3AF"
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        var byIncomeCategory = transactions
            .Where(t => t.Type == TransactionType.Income) 
            .GroupBy(t => new { t.Category.Name, t.Category.Color })
            .Select(g => new CategorySummaryDto
            {
                Name = g.Key.Name,
                Value = Math.Round(g.Sum(t => t.Amount), 2),
                Fill = g.Key.Color ?? "#C6FF5E" 
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        return new DashboardDataDto
        {
            ProtocolFlow = byType,
            CategoryExpenseAllocation = byExpenseCategory,
            CategoryIncomeAllocation = byIncomeCategory
        };
    }

private IQueryable<Transaction> ApplyFilters(IQueryable<Transaction> query, TransactionFilterDto filter)
{
    if (filter.FromDate.HasValue) 
        query = query.Where(t => t.TransactionDate >= filter.FromDate.Value);
    if (filter.ToDate.HasValue)
        query = query.Where(t => t.TransactionDate <= filter.ToDate.Value);
    if (filter.CategoryId.HasValue) 
        query = query.Where(t => t.CategoryId == filter.CategoryId.Value);

    if (!string.IsNullOrEmpty(filter.SearchTerm))
        query = query.Where(t => t.Description.Contains(filter.SearchTerm)
                                 || (t.Notes != null && t.Notes.Contains(filter.SearchTerm)));
    
    return query;
}
}