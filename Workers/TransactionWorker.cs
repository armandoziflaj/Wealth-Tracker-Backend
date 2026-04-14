using Microsoft.EntityFrameworkCore;
using WealthTracker.Models;

namespace WealthTracker.Workers;

public sealed class RecurringTransactionWorker(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1).AddHours(1); 
            var delay = nextRun - now;
            if (delay.TotalMilliseconds <= 0) delay = TimeSpan.FromDays(1);

            await Task.Delay(delay, stoppingToken);

            try 
            {
                await ProcessRecurringTransactions();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Recurring Job Failed: {ex.Message}");
            }
        }
    }

    private async Task ProcessRecurringTransactions()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var today = DateTime.UtcNow.Date;

        var recurringConfigs = await context.Transactions
            .Where(t => t.IsRecurring && t.NextOccurrence != null && t.NextOccurrence <= today)
            .ToListAsync();

        if (!recurringConfigs.Any()) return;

        foreach (var config in recurringConfigs)
        {
            var newTransaction = new Transaction
            {
                Amount = config.Amount,
                Description = config.Description,
                CategoryId = config.CategoryId,
                TransactionDate = config.NextOccurrence ?? today, 
                Type = config.Type,
                IsRecurring = false,
                ParentTransactionId = config.Id,
                UserId = config.UserId,
                Notes = $"Recurring Transaction"
            };

            context.Transactions.Add(newTransaction);
            config.SetupNextOccurrence();
        }

        await context.SaveChangesAsync();
    }
}