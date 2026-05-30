using CafeManagement.Application.Abstractions;
using CafeManagement.Application.Contracts.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace CafeManagement.Api.Controllers;

[ApiController]
[Route("api/v1/catalog")]
public sealed class CatalogController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public Task<CatalogResponseDto> Get([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
        => catalogService.GetCatalogAsync(includeInactive, cancellationToken);

    [HttpPost("products")]
    public Task<ProductDto> UpsertProduct([FromBody] UpsertProductRequest request, CancellationToken cancellationToken)
        => catalogService.UpsertProductAsync(request, cancellationToken);

    [HttpDelete("products/{productId:int}")]
    public async Task<IActionResult> DeleteProduct(int productId, CancellationToken cancellationToken)
        => await catalogService.DeleteProductAsync(productId, cancellationToken) ? Ok() : NotFound();

    [HttpPost("categories")]
    public Task<CategoryDto> UpsertCategory([FromBody] UpsertCategoryRequest request, CancellationToken cancellationToken)
        => catalogService.UpsertCategoryAsync(request, cancellationToken);

    [HttpDelete("categories/{categoryId:int}")]
    public async Task<IActionResult> DeleteCategory(int categoryId, CancellationToken cancellationToken)
        => await catalogService.DeleteCategoryAsync(categoryId, cancellationToken) ? Ok() : NotFound();
}
