using CafeManagement.Application.Contracts.Settings;

namespace CafeManagement.Application.Abstractions;

public interface ISettingsService
{
    Task<AppSettingsDto> GetAppSettingsAsync(CancellationToken cancellationToken = default);
    Task<AppSettingsDto> UpdateAppSettingsAsync(UpdateAppSettingsRequest request, CancellationToken cancellationToken = default);
    Task<InfoMessageDto?> GetActiveInfoMessageAsync(CancellationToken cancellationToken = default);
    Task<InfoMessageDto> UpsertInfoMessageAsync(UpdateInfoMessageRequest request, CancellationToken cancellationToken = default);
}
