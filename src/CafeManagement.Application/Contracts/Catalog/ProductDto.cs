namespace CafeManagement.Application.Contracts.Catalog;

public sealed record ProductDto(int Id, int CategoryId, string Name, string? Description, decimal Price, string? ImageUrl, bool IsActive);
