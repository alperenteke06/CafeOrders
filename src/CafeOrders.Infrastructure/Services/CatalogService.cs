using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Catalog;
using CafeOrders.Domain.Entities;
using CafeOrders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Infrastructure.Services;

public sealed class CatalogService(
    CafeOrdersDbContext dbContext,
    IRealtimeNotifier realtimeNotifier) : ICatalogService
{
    public async Task<CatalogResponseDto> GetCatalogAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var categoriesQuery = dbContext.Categories
            .AsNoTracking()
            .Where(x => !x.IsDeleted);
        var productsQuery = dbContext.Products
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!includeInactive)
        {
            categoriesQuery = categoriesQuery.Where(x => x.IsActive);
            productsQuery = productsQuery.Where(x => x.IsActive);
        }

        var categories = await categoriesQuery.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        var products = await productsQuery.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return new CatalogResponseDto(categories.Select(x => x.ToDto()).ToArray(), products.Select(x => x.ToDto()).ToArray());
    }

    public async Task<ProductDto> UpsertProductAsync(UpsertProductRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Urun adi bos birakilamaz.");
        }

        if (request.Price <= 0)
        {
            throw new InvalidOperationException("Urun fiyati sifirdan buyuk olmalidir.");
        }

        var category = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == request.CategoryId && !x.IsDeleted, cancellationToken);
        if (category is null)
        {
            throw new InvalidOperationException("Secilen kategori bulunamadi.");
        }

        var product = request.Id.HasValue
            ? await dbContext.Products.FirstOrDefaultAsync(x => x.Id == request.Id.Value && !x.IsDeleted, cancellationToken)
            : null;

        if (product is null)
        {
            product = new Product();
            await dbContext.Products.AddAsync(product, cancellationToken);
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Price = request.Price;
        product.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        product.IsActive = request.IsActive;
        product.IsDeleted = false;

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyCatalogUpdatedAsync(cancellationToken);
        return product.ToDto();
    }

    public async Task<bool> DeleteProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == productId && !x.IsDeleted, cancellationToken);
        if (product is null)
        {
            return false;
        }

        product.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyCatalogUpdatedAsync(cancellationToken);
        return true;
    }

    public async Task<CategoryDto> UpsertCategoryAsync(UpsertCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Kategori adi bos birakilamaz.");
        }

        if (request.SortOrder < 0)
        {
            throw new InvalidOperationException("Kategori sirasi sifirdan kucuk olamaz.");
        }

        var category = request.Id.HasValue
            ? await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == request.Id.Value && !x.IsDeleted, cancellationToken)
            : null;

        var duplicateOrderExists = await dbContext.Categories.AnyAsync(
            x => !x.IsDeleted && x.SortOrder == request.SortOrder && x.Id != request.Id,
            cancellationToken);
        if (duplicateOrderExists)
        {
            throw new InvalidOperationException("Ayni sira numarasina sahip baska bir kategori bulunuyor.");
        }

        if (category is null)
        {
            category = new Category();
            await dbContext.Categories.AddAsync(category, cancellationToken);
        }

        category.Name = request.Name.Trim();
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        category.IsDeleted = false;

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyCatalogUpdatedAsync(cancellationToken);
        return category.ToDto();
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == categoryId && !x.IsDeleted, cancellationToken);
        if (category is null)
        {
            return false;
        }

        category.IsDeleted = true;
        foreach (var product in category.Products)
        {
            product.IsDeleted = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyCatalogUpdatedAsync(cancellationToken);
        return true;
    }
}
