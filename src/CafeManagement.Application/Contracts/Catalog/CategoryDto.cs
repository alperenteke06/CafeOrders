namespace CafeManagement.Application.Contracts.Catalog;

public sealed record CategoryDto(int Id, string Name, int SortOrder, bool IsActive);
