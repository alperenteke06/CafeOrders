using System.Net.Http;
using System.Net.Http.Json;
using CafeOrders.Application.Contracts.Catalog;
using CafeOrders.Application.Contracts.Devices;
using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Application.Contracts.Settings;

namespace CafeOrders.DesktopApp.Services;

public sealed class ClientApiService(HttpClient httpClient)
{
    public async Task<DeviceRegistrationResponse?> RegisterAsync(DeviceRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/v1/devices/register", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DeviceRegistrationResponse>(cancellationToken: cancellationToken);
    }

    public Task<HttpResponseMessage> HeartbeatAsync(HeartbeatRequest request, CancellationToken cancellationToken = default)
        => httpClient.PostAsJsonAsync("api/v1/devices/heartbeat", request, cancellationToken);

    public async Task<CatalogResponseDto> GetCatalogAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<CatalogResponseDto>("api/v1/catalog", cancellationToken)
           ?? new CatalogResponseDto(Array.Empty<CategoryDto>(), Array.Empty<ProductDto>());

    public async Task<AppSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<AppSettingsDto>("api/v1/settings/app", cancellationToken)
           ?? new AppSettingsDto("NightByte Lounge", "Alperen TEKE", "0 (541) 688 88 06", string.Empty, string.Empty, string.Empty, "Info", "campaign", false, false, false, null);

    public async Task<InfoMessageDto?> GetActiveInfoMessageAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("api/v1/settings/info-message", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InfoMessageDto>(cancellationToken: cancellationToken);
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/v1/orders", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("Siparis yaniti alinamadi.");
    }

    public async Task<OrderDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/v1/orders/{orderId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: cancellationToken);
    }
}
