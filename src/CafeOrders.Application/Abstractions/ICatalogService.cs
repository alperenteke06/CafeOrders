using CafeOrders.Application.Contracts.Catalog;

namespace CafeOrders.Application.Abstractions;

public interface ICatalogService
{
    Task<CatalogResponseDto> GetCatalogAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<ProductDto> UpsertProductAsync(UpsertProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(int productId, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpsertCategoryAsync(UpsertCategoryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
}
