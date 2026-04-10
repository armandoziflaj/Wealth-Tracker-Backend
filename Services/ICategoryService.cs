using WealthTracker.Models;
using WealthTracker.Requests;
using WealthTracker.Responses;

namespace WealthTracker.Services;

public interface ICategoryService
{
    Task<BaseResponse<IEnumerable<CategoriesResponses>?>> Get(long userId, CancellationToken cancellationToken = default);
    Task<BaseResponse<CategoriesResponses?>> GetById(long userId, long id, CancellationToken cancellationToken = default);
    Task<Category> CreateAsync(long userId, CategoryCreateDto dto, CancellationToken cancellationToken = default);
    Task<Category?> UpdateAsync(long userId, CategoryUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long userId, long id, CancellationToken cancellationToken = default);
}