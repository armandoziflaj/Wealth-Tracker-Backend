using Microsoft.EntityFrameworkCore;
using WealthTracker.Models;
using WealthTracker.Requests;
using WealthTracker.Responses;

namespace WealthTracker.Services;

public class CategoryService (ApplicationDbContext context) : ICategoryService
{
    public async Task<BaseResponse<IEnumerable<CategoriesResponses>?>> Get(long userId, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories
                                                        .Where(c => c.UserId == userId)
                                                        .Select(x => new CategoriesResponses()
                                                        {
                                                            Id = x.Id,
                                                            Name = x.Name,
                                                            Color = x.Color,
                                                            Type = x.Type,
                                                            TransactionTotal = x.Transactions.Any() ? x.Transactions.Sum(t => t.Amount) : 0
                                                            
                                                        })
                                                        .ToListAsync(cancellationToken);
        return category;
    }
    public async Task<BaseResponse<CategoriesResponses?>> GetById(long userId, long id, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories
                                                      .Where(c => c.UserId == userId && c.Id == id)
                                                      .Select(x => new CategoriesResponses()
                                                      {
                                                        Id = x.Id,
                                                        Name = x.Name,
                                                        Color = x.Color,
                                                        UserId = x.UserId
                                                      }).FirstOrDefaultAsync(cancellationToken);
        return category;
    }
    
    public async Task<Category> CreateAsync(long userId, CategoryCreateDto dto, CancellationToken cancellationToken = default)
    {
        var exists = await context.Categories
            .AnyAsync(c => c.UserId == userId && c.Name == dto.Name, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"A category with the name '{dto.Name}' already exists.");
        }

        var colorExists = await context.Categories
            .AnyAsync(c => c.UserId == userId && c.Color == dto.Color, cancellationToken);
    
        if (colorExists)
        {
            throw new InvalidOperationException("This color signature is already assigned to another node.");
        }

        var category = new Category
        {
            Name = dto.Name,
            Type = dto.Type,
            Color = dto.Color,
            UserId = userId
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);
    
        return category;
    }

    public async Task<Category?> UpdateAsync(long userId, CategoryUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == dto.Id && c.UserId == userId, cancellationToken);

        if (category == null) return null;

        if (!string.IsNullOrEmpty(dto.Color) && dto.Color != category.Color &&
            await context.Categories.AnyAsync(c => c.UserId == userId && c.Color == dto.Color, cancellationToken))
        {
            throw new Exception("Color already in use.");
        }

        category.Name = dto.Name;
        category.Type = dto.Type;
        category.Color = dto.Color;

        await context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<bool> DeleteAsync(long userId, long id, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);

        if (category == null) 
            return false;

        var hasTransactions = 
            await context.Transactions.AnyAsync(t => t.CategoryId == id, cancellationToken);
        
        if (hasTransactions)
        {
            throw new Exception("Cannot delete category with existing transactions.");
        }

        context.Categories.Remove(category);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}