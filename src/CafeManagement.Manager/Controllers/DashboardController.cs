using CafeManagement.Application.Abstractions;
using CafeManagement.Application.Contracts.Catalog;
using CafeManagement.Application.Contracts.Devices;
using CafeManagement.Application.Contracts.Settings;
using CafeManagement.Application.Contracts.Tables;
using CafeManagement.Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CafeManagement.Manager.Controllers;

[Authorize]
public sealed class DashboardController(
    IDashboardService dashboardService,
    ICatalogService catalogService,
    IOrderService orderService,
    ISettingsService settingsService,
    ITableService tableService,
    IWebHostEnvironment webHostEnvironment,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : Controller
{
    private static readonly JsonSerializerOptions ApiJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif"
    };

    private static readonly HashSet<string> ValidSections =
    [
        "dashboard",
        "orders",
        "products",
        "categories",
        "devices",
        "tables",
        "settings",
        "notifications"
    ];

    [HttpGet("/")]
    public async Task<IActionResult> Index(
        [FromQuery] string? section,
        [FromQuery] string? range,
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int? page,
        CancellationToken cancellationToken)
    {
        var activeSection = NormalizeSection(section);
        var viewModel = await BuildViewModelAsync(activeSection, range, search, category, page, cancellationToken);

        return View(viewModel);
    }

    [HttpGet("/dashboard/section/{section}")]
    public async Task<IActionResult> Section(
        string section,
        [FromQuery] string? range,
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int? page,
        CancellationToken cancellationToken)
    {
        var activeSection = NormalizeSection(section);
        var viewModel = await BuildViewModelAsync(activeSection, range, search, category, page, cancellationToken);

        return PartialView(GetSectionViewName(activeSection), viewModel);
    }

    [HttpGet("/dashboard/live")]
    public async Task<IActionResult> Live(CancellationToken cancellationToken)
        => Json(await dashboardService.GetSnapshotAsync(cancellationToken));

    [HttpGet("/dashboard/presentation")]
    public async Task<IActionResult> Presentation(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAppSettingsAsync(cancellationToken);
        var snapshot = await dashboardService.GetSnapshotAsync(cancellationToken);

        return Json(new
        {
            cafeName = settings.CafeName,
            appDeveloperName = settings.AppDeveloperName,
            appDeveloperPhone = settings.AppDeveloperPhone,
            footerCopyright = $"{settings.CafeName} @ {DateTime.Now.Year} Tum Haklari Saklidir.",
            soundEnabled = settings.EnableNewOrderSound,
            soundUrl = settings.NewOrderSoundUrl,
            infoMessage = snapshot.ActiveInfoMessage is null
                ? new
                {
                    message = settings.ClientInfoBoxMessage,
                    type = settings.ClientInfoBoxType,
                    iconKey = settings.ClientInfoBoxIcon
                }
                : new
                {
                    message = snapshot.ActiveInfoMessage.Message,
                    type = snapshot.ActiveInfoMessage.Type,
                    iconKey = snapshot.ActiveInfoMessage.IconKey
                }
        });
    }

    [HttpPost("/dashboard/devices/approve")]
    public async Task<IActionResult> ApproveDevice([FromBody] ApproveDeviceRequest request, CancellationToken cancellationToken)
    {
        var response = await CreateApiClient().PostAsJsonAsync("/api/v1/devices/approve", request, cancellationToken);
        return await ToApiActionResultAsync(response, cancellationToken);
    }

    [HttpPost("/dashboard/orders/{orderId:int}/accept")]
    public async Task<IActionResult> AcceptOrder(int orderId, CancellationToken cancellationToken)
    {
        var response = await CreateApiClient().PostAsync($"/api/v1/orders/{orderId}/accept", content: null, cancellationToken);
        return await ToApiActionResultAsync(response, cancellationToken);
    }

    [HttpPost("/dashboard/orders/{orderId:int}/reject")]
    public async Task<IActionResult> RejectOrder(int orderId, CancellationToken cancellationToken)
    {
        var response = await CreateApiClient().PostAsync($"/api/v1/orders/{orderId}/reject", content: null, cancellationToken);
        return await ToApiActionResultAsync(response, cancellationToken);
    }

    [HttpPost("/dashboard/orders/{orderId:int}/complete")]
    public async Task<IActionResult> CompleteOrder(int orderId, CancellationToken cancellationToken)
    {
        var response = await CreateApiClient().PostAsync($"/api/v1/orders/{orderId}/complete", content: null, cancellationToken);
        return await ToApiActionResultAsync(response, cancellationToken);
    }

    [HttpPost("/dashboard/info-message")]
    public Task<IActionResult> UpdateInfoMessage([FromBody] UpdateInfoMessageRequest request, CancellationToken cancellationToken)
        => UpdateInfoMessageCore(request, cancellationToken);

    [HttpPut("/dashboard/settings/app")]
    public async Task<IActionResult> UpdateAppSettings([FromBody] UpdateAppSettingsRequest request, CancellationToken cancellationToken)
    {
        var response = await CreateApiClient().PutAsJsonAsync("/api/v1/settings/app", request, cancellationToken);
        return await ToApiActionResultAsync(response, cancellationToken);
    }

    [HttpPost("/dashboard/settings/upload-sound")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadSound(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Ses dosyasi secilmedi." });
        }

        if (!file.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Yalnizca ses dosyalari yuklenebilir." });
        }

        var uploadsDirectory = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "sounds");
        Directory.CreateDirectory(uploadsDirectory);

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".mp3";
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadsDirectory, fileName);

        await using (var stream = System.IO.File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return Json(new
        {
            url = $"{Request.Scheme}://{Request.Host}/uploads/sounds/{fileName}",
            fileName = file.FileName
        });
    }

    [HttpPost("/dashboard/products/upload-image")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadProductImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Gorsel dosyasi secilmedi." });
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var hasAllowedExtension = !string.IsNullOrWhiteSpace(extension) && AllowedImageExtensions.Contains(extension);
        var hasImageContentType = HasExpectedContentType(file.ContentType, "image/");
        if (!hasAllowedExtension && !hasImageContentType)
        {
            return BadRequest(new { message = "Yalnizca JPG, PNG, WEBP veya GIF gorselleri yuklenebilir." });
        }

        extension = hasAllowedExtension ? extension : ".png";
        var uploadsDirectory = Path.Combine(ResolveWebRootPath(), "uploads", "products");
        Directory.CreateDirectory(uploadsDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadsDirectory, fileName);

        await using (var stream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return Json(new
        {
            url = $"{Request.Scheme}://{Request.Host}/uploads/products/{Uri.EscapeDataString(fileName)}",
            fileName = file.FileName
        });
    }

    [HttpPost("/dashboard/products")]
    public async Task<IActionResult> UpsertProduct([FromBody] UpsertProductRequest request, CancellationToken cancellationToken)
    {
        var response = await CreateApiClient().PostAsJsonAsync("/api/v1/catalog/products", request, cancellationToken);
        return await ToApiActionResultAsync(response, cancellationToken);
    }

    [HttpPost("/dashboard/products/bulk-prices")]
    public async Task<IActionResult> UpdateProductPrices([FromBody] BulkProductPriceUpdateBatchRequest request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            return BadRequest(new { message = "Fiyat guncellemesi icin en az bir urun secilmelidir." });
        }

        using var apiClient = CreateApiClient();
        foreach (var item in request.Items)
        {
            var payload = new UpsertProductRequest(
                item.Id,
                item.CategoryId,
                item.Name,
                item.Description,
                item.Price,
                item.ImageUrl,
                item.IsActive);

            var response = await apiClient.PostAsJsonAsync("/api/v1/catalog/products", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return await ToApiActionResultAsync(response, cancellationToken);
            }
        }

        return Json(new { updatedCount = request.Items.Count });
    }

    [HttpDelete("/dashboard/products/{productId:int}")]
    public async Task<IActionResult> DeleteProduct(int productId, CancellationToken cancellationToken)
        => await ToApiActionResultAsync(await CreateApiClient().DeleteAsync($"/api/v1/catalog/products/{productId}", cancellationToken), cancellationToken);

    [HttpGet("/dashboard/catalog/categories")]
    public async Task<IActionResult> GetProductCategoryOptions(CancellationToken cancellationToken)
    {
        var catalog = await catalogService.GetCatalogAsync(includeInactive: true, cancellationToken: cancellationToken);
        var categories = catalog.Categories
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new { id = x.Id, name = x.Name })
            .ToArray();

        return Json(new { categories });
    }

    [HttpPost("/dashboard/categories")]
    public async Task<IActionResult> UpsertCategory([FromBody] UpsertCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await CreateApiClient().PostAsJsonAsync("/api/v1/catalog/categories", request, cancellationToken);
        return await ToApiActionResultAsync(response, cancellationToken);
    }

    [HttpDelete("/dashboard/categories/{categoryId:int}")]
    public async Task<IActionResult> DeleteCategory(int categoryId, CancellationToken cancellationToken)
        => await ToApiActionResultAsync(await CreateApiClient().DeleteAsync($"/api/v1/catalog/categories/{categoryId}", cancellationToken), cancellationToken);

    [HttpPost("/dashboard/tables")]
    public async Task<IActionResult> UpsertTable([FromBody] UpsertTableRequest request, CancellationToken cancellationToken)
    {
        var response = await CreateApiClient().PostAsJsonAsync("/api/v1/tables", request, cancellationToken);
        return await ToApiActionResultAsync(response, cancellationToken);
    }

    [HttpPost("/dashboard/devices/assign-table")]
    public async Task<IActionResult> AssignDeviceTable([FromBody] AssignDeviceTableRequest request, CancellationToken cancellationToken)
    {
        var response = await CreateApiClient().PostAsJsonAsync("/api/v1/devices/assign-table", request, cancellationToken);
        return await ToApiActionResultAsync(response, cancellationToken);
    }

    [HttpDelete("/dashboard/devices/{deviceId:guid}")]
    public async Task<IActionResult> RejectDevice(Guid deviceId, CancellationToken cancellationToken)
        => await ToApiActionResultAsync(await CreateApiClient().DeleteAsync($"/api/v1/devices/{deviceId}", cancellationToken), cancellationToken);

    private async Task<DashboardViewModel> BuildViewModelAsync(
        string activeSection,
        string? range,
        string? search,
        string? category,
        int? page,
        CancellationToken cancellationToken)
    {
        var snapshot = await dashboardService.GetSnapshotAsync(cancellationToken);
        var catalog = await catalogService.GetCatalogAsync(includeInactive: true, cancellationToken: cancellationToken);
        var appSettings = await settingsService.GetAppSettingsAsync(cancellationToken);
        var tables = await tableService.GetTablesAsync(cancellationToken);
        var allRecentOrders = await orderService.GetRecentOrdersAsync(500, cancellationToken);
        var categoryMap = catalog.Categories.ToDictionary(x => x.Id, x => x.Name);
        var selectedRange = NormalizeRange(range);
        var searchQuery = search?.Trim() ?? string.Empty;
        var currentPage = Math.Max(page ?? 1, 1);
        var categoryFilter = string.IsNullOrWhiteSpace(category) ? "all" : category.Trim().ToLowerInvariant();
        var rangeStart = selectedRange switch
        {
            "1a" => DateTime.UtcNow.AddMonths(-1),
            "1h" => DateTime.UtcNow.AddDays(-7),
            _ => DateTime.UtcNow.AddDays(-1)
        };

        var recentOrders = allRecentOrders
            .Where(order => order.CreatedAt >= rangeStart)
            .Where(order => string.IsNullOrWhiteSpace(searchQuery) || OrderMatchesSearch(order, searchQuery))
            .OrderByDescending(order => order.Status == "Pending")
            .ThenByDescending(order => order.CreatedAt)
            .ToArray();

        var notifications = allRecentOrders
            .Where(order => string.IsNullOrWhiteSpace(searchQuery) || OrderMatchesSearch(order, searchQuery))
            .OrderByDescending(GetNotificationTimestamp)
            .Take(100)
            .Select(MapNotification)
            .ToArray();

        return new DashboardViewModel
        {
            ActiveSection = activeSection,
            SelectedRange = selectedRange,
            SearchQuery = searchQuery,
            CategoryFilter = categoryFilter,
            CurrentPage = currentPage,
            Snapshot = snapshot,
            Catalog = catalog,
            AppSettings = appSettings,
            Tables = tables,
            RecentOrders = recentOrders,
            Notifications = notifications,
            ProductCards = catalog.Products.Select(product => new ProductCardViewModel
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                CategoryName = categoryMap.TryGetValue(product.CategoryId, out var categoryName) ? categoryName : "Kategori",
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                Price = product.Price,
                VisualClass = GetVisualClass(product.Name, product.Id),
                InStock = product.IsActive
            }).ToArray()
        };
    }

    private async Task<IActionResult> UpdateInfoMessageCore(UpdateInfoMessageRequest request, CancellationToken cancellationToken)
        => await ToApiActionResultAsync(await CreateApiClient().PutAsJsonAsync("/api/v1/settings/info-message", request, cancellationToken), cancellationToken);

    private string ResolveWebRootPath()
    {
        if (!string.IsNullOrWhiteSpace(webHostEnvironment.WebRootPath))
        {
            return webHostEnvironment.WebRootPath;
        }

        return Path.Combine(webHostEnvironment.ContentRootPath, "wwwroot");
    }

    private static bool HasExpectedContentType(string? contentType, string expectedContentTypePrefix)
        => !string.IsNullOrWhiteSpace(contentType) &&
           contentType.StartsWith(expectedContentTypePrefix, StringComparison.OrdinalIgnoreCase);

    private HttpClient CreateApiClient()
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(ResolveApiBaseUrl());
        return client;
    }

    private string ResolveApiBaseUrl()
    {
        var configured = configuration["ApiBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        var host = Request.Host.Host;
        var isHttps = Request.IsHttps;
        var port = isHttps ? 7001 : 5001;
        return $"{(isHttps ? "https" : "http")}://{host}:{port}";
    }

    private async Task<IActionResult> ToApiActionResultAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            if (response.Content.Headers.ContentLength is null or 0)
            {
                return Ok();
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions, cancellationToken);
            return json.ValueKind == JsonValueKind.Undefined ? Ok() : Json(json);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        var message = await ResolveApiErrorMessageAsync(response, cancellationToken);
        return StatusCode((int)response.StatusCode, new { message });
    }

    private static async Task<string> ResolveApiErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("message", out var messageElement))
            {
                var message = messageElement.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }
        }
        catch
        {
        }

        return $"Islem basarisiz oldu. HTTP {(int)response.StatusCode}";
    }

    private static string NormalizeSection(string? section)
        => !string.IsNullOrWhiteSpace(section) && ValidSections.Contains(section)
            ? section
            : "dashboard";

    private static string NormalizeRange(string? range)
        => range?.Trim().ToLowerInvariant() switch
        {
            "1h" => "1h",
            "1a" => "1a",
            _ => "1g"
        };

    private static string GetSectionViewName(string activeSection)
        => activeSection switch
        {
            "orders" => "_OrdersSection",
            "products" => "_ProductsSection",
            "categories" => "_CategoriesSection",
            "devices" => "_DevicesSection",
            "tables" => "_TablesSection",
            "settings" => "_SettingsSection",
            "notifications" => "_NotificationsSection",
            _ => "_DashboardSection"
        };

    private static string GetVisualClass(string productName, int productId)
    {
        var name = productName.ToLowerInvariant();
        if (name.Contains("pizza"))
        {
            return "visual-pizza";
        }

        if (name.Contains("burger"))
        {
            return "visual-burger";
        }

        if (name.Contains("kahve") || name.Contains("latte") || name.Contains("brew"))
        {
            return "visual-coffee";
        }

        if (name.Contains("icecek") || name.Contains("çay") || name.Contains("cay") || name.Contains("enerji"))
        {
            return "visual-drink";
        }

        if (name.Contains("patates") || name.Contains("nacho"))
        {
            return "visual-snack";
        }

        return productId % 2 == 0 ? "visual-burger" : "visual-snack";
    }

    private static bool OrderMatchesSearch(Application.Contracts.Orders.OrderDto order, string searchQuery)
    {
        if (order.Id.ToString().Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (order.TableId.ToString().Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (order.Status.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return order.Lines.Any(line => line.ProductName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
    }

    private static DateTime GetNotificationTimestamp(Application.Contracts.Orders.OrderDto order)
        => order.Status switch
        {
            "Completed" => order.CompletedAt ?? order.AcceptedAt ?? order.CreatedAt,
            "Accepted" => order.AcceptedAt ?? order.CreatedAt,
            "Rejected" => order.RejectedAt ?? order.CreatedAt,
            _ => order.CreatedAt
        };

    private static NotificationItemViewModel MapNotification(Application.Contracts.Orders.OrderDto order)
    {
        var status = order.Status;
        var timestamp = GetNotificationTimestamp(order).ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        var title = status switch
        {
            "Pending" => $"Masa {order.TableId} yeni siparis gonderdi",
            "Accepted" => $"Masa {order.TableId} siparisi onaylandi",
            "Rejected" => $"Masa {order.TableId} siparisi reddedildi",
            "Completed" => $"Masa {order.TableId} siparisi tamamlandi",
            _ => $"Masa {order.TableId} siparis guncellendi"
        };

        return new NotificationItemViewModel
        {
            OrderId = order.Id,
            Title = title,
            Meta = timestamp,
            Amount = order.TotalPrice.ToString("C0"),
            Status = status
        };
    }
}

public sealed record BulkProductPriceUpdateBatchRequest(IReadOnlyList<BulkProductPriceUpdateItemRequest> Items);

public sealed record BulkProductPriceUpdateItemRequest(
    int? Id,
    int CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    bool IsActive);
