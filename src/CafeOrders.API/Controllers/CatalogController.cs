using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrders.API.Controllers;

[ApiController]
[Route("api/v1/catalog")]
public sealed class CatalogController(ICatalogService catalogService, ILogger<CatalogController> logger) : ControllerBase
{
    [HttpGet]
    public Task<CatalogResponseDto> Get([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
        => catalogService.GetCatalogAsync(includeInactive, cancellationToken);

    [HttpPost("products")]
    public async Task<ProductDto> UpsertProduct([FromBody] UpsertProductRequest request, CancellationToken cancellationToken)
    {
        var product = await catalogService.UpsertProductAsync(request, cancellationToken);
        logger.LogInformation("Product upserted. ProductId={ProductId}, Name={Name}, CategoryId={CategoryId}, Price={Price}, IsActive={IsActive}", product.Id, product.Name, product.CategoryId, product.Price, product.IsActive);
        return product;
    }

    [HttpDelete("products/{productId:int}")]
    public async Task<IActionResult> DeleteProduct(int productId, CancellationToken cancellationToken)
    {
        var deleted = await catalogService.DeleteProductAsync(productId, cancellationToken);
        if (deleted)
        {
            logger.LogInformation("Product deleted. ProductId={ProductId}", productId);
        }
        return deleted ? Ok() : NotFound();
    }

    [HttpPost("categories")]
    public async Task<CategoryDto> UpsertCategory([FromBody] UpsertCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await catalogService.UpsertCategoryAsync(request, cancellationToken);
        logger.LogInformation("Category upserted. CategoryId={CategoryId}, Name={Name}, SortOrder={SortOrder}, IsActive={IsActive}", category.Id, category.Name, category.SortOrder, category.IsActive);
        return category;
    }

    [HttpDelete("categories/{categoryId:int}")]
    public async Task<IActionResult> DeleteCategory(int categoryId, CancellationToken cancellationToken)
    {
        var deleted = await catalogService.DeleteCategoryAsync(categoryId, cancellationToken);
        if (deleted)
        {
            logger.LogInformation("Category deleted. CategoryId={CategoryId}", categoryId);
        }
        return deleted ? Ok() : NotFound();
    }
}
