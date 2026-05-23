using CommunityToolkit.Mvvm.ComponentModel;

namespace CafeOrders.DesktopApp.Models;

public sealed partial class CategoryItem : ObservableObject
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}

public sealed partial class ProductItem : ObservableObject
{
    public int Id { get; init; }
    public int CategoryId { get; init; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private string? _imageUrl;

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

    partial void OnImageUrlChanged(string? value)
    {
        OnPropertyChanged(nameof(HasImage));
    }
}

public sealed partial class CartItem : ObservableObject
{
    public int ProductId { get; init; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string? _imageUrl;

    [ObservableProperty]
    private decimal _unitPrice;

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

    [ObservableProperty]
    private int _quantity;

    public decimal Total => UnitPrice * Quantity;

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(Total));
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(Total));
    }

    partial void OnImageUrlChanged(string? value)
    {
        OnPropertyChanged(nameof(HasImage));
    }
}
