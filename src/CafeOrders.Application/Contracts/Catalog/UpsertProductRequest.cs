namespace CafeOrders.Application.Contracts.Catalog;

public sealed record UpsertProductRequest(
    int? Id,
    int CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    bool IsActive);
