using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WealthTracker.Requests;
using WealthTracker.Services;

namespace WealthTracker.Controllers;

[Authorize]
public class CategoryController (ICategoryService categoryService) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        
        var result = await categoryService.Get(userId, cancellationToken);
        
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        
        var result = await categoryService.GetById(userId, id, cancellationToken);
        
        return Success(result);
    } 
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryCreateDto dto, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        
        var result = await categoryService.CreateAsync(userId, dto, cancellationToken);
        
        return Success(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update( [FromBody]  CategoryUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        
        var result = await categoryService.UpdateAsync(userId, dto, cancellationToken);
        
        return result == null ? BadRequest("Category not found or access denied.") 
                              : Success(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        
        var result = await categoryService.DeleteAsync(userId, id, cancellationToken);
        
        return !result ? BadRequest("Could not delete category. Check if transaction exists.") 
                       : Success(result);
    }
}