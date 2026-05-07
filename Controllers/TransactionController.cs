using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WealthTracker.Requests;
using WealthTracker.Responses;
using WealthTracker.Services;

namespace WealthTracker.Controllers;

[Authorize]
public class TransactionController(ITransactionService transactionService) : BaseController
{
    [HttpGet("GetTransactions")]
    public async Task<IActionResult> GetTransactions(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        
        var result = await transactionService.GetAllAsync(userId, cancellationToken);
        
        return Success(result);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransactionCreateDto transaction, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        
        var createdTransaction = await transactionService.CreateAsync(userId, transaction, cancellationToken);
        
        return Success(createdTransaction);
    }
    
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] TransactionUpdateDto transaction, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        
        var updatedTransaction = await transactionService.UpdateAsync(userId, transaction, cancellationToken);
        
        return (updatedTransaction is null) ? BadRequest("Transaction not found") : Success(updatedTransaction);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        
        var createdTransaction = await transactionService.DeleteAsync(userId, id, cancellationToken);
        
        return Success(createdTransaction);
    }
    
    [HttpGet("GetFilteredTransactions")]
    public async Task<IActionResult> GetFilteredTransactions([FromQuery] TransactionFilterDto  filters, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        
        var result = await transactionService.Filter(userId, filters, cancellationToken);
        
        var response = PaginatedResponse<TransactionResponseDto>.Success(
            result.Items, 
            result.TotalCount, 
            filters.PageNumber, 
            filters.PageSize
        );
        
        return Ok(response);
    }
    
    [HttpGet("GetMonthlySummary")]
    public async Task<IActionResult> GetMonthlySummary(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        
        var result = await transactionService.GetMonthlySummary(userId, cancellationToken);
        
        return Success(result);
    }
    
    [HttpGet("GetCategorySummary")]
    public async Task<IActionResult> GetCategorySummary(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        
        var result = await transactionService.GetCategorySummary(userId, cancellationToken);
        
        return Success(result);
    }
}