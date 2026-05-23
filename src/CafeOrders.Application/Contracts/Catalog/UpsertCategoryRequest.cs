namespace CafeOrders.Application.Contracts.Catalog;

public sealed record UpsertCategoryRequest(int? Id, string Name, int SortOrder, bool IsActive);
