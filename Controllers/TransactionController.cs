using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WealthTracker.Models;
using WealthTracker.Requests;
using WealthTracker.Services;

namespace WealthTracker.Controllers;

[Authorize]
public class TransactionController(ITransactionService transactionService) : BaseController
{
    [HttpGet("GetTransactions")]
    public async Task<IActionResult> Register()
    {
        var userId = GetUserId();
        
        var result = await transactionService.GetAllAsync(userId);
        
        return CreateSuccessResponse(result);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Transaction transaction)
    {
        var userId = GetUserId();
        
        var createdTransaction = await transactionService.CreateAsync(userId, transaction);
        
        return createdTransaction == null ? CreateErrorResponse<string>(["Could not add the transaction"], "Save Error") 
                                          :  CreateSuccessResponse(createdTransaction);
    }
    
    [HttpGet("GetFilteredTransactions")]
    public async Task<IActionResult> GetFilteredTransactions(TransactionFilterDto  filters)
    {
        var userId = GetUserId();
        
        var result = await transactionService.Filter(userId, filters);
        
        return CreateSuccessResponse(result);
    }
}