using CafeOrders.Application.Contracts.Logging;

namespace CafeOrders.Application.Abstractions;

public interface IApplicationLogService
{
    Task<ApplicationLogDto> CreateAsync(ApplicationLogCreateRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ApplicationLogDto>> GetRecentAsync(
        string? source = null,
        string? level = null,
        string? search = null,
        int take = 200,
        CancellationToken cancellationToken = default);
}
