namespace CafeManagement.Application.Contracts.Catalog;

public sealed record CatalogResponseDto(IReadOnlyCollection<CategoryDto> Categories, IReadOnlyCollection<ProductDto> Products);
