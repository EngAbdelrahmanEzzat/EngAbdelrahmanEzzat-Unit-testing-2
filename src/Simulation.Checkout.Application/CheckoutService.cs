using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Domain.Exceptions;

namespace Simulation.Checkout.Application;

public sealed class CheckoutService
{
    public const decimal DiscountThreshold = 1000m;
    public const decimal DiscountRate = 0.10m;
    public const decimal MaxDiscount = 100m;

    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryService _inventoryService;
    private readonly INotificationService _notificationService;

    public CheckoutService(
        IOrderRepository orderRepository,
        IInventoryService inventoryService,
        INotificationService notificationService)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }
    public async Task<CheckoutReceipt> CheckoutAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        ValidateOrder(order);

        await EnsureInventoryAsync(order, cancellationToken);

        var subtotal = CalculateSubtotal(order);
        var discount = CalculateDiscount(subtotal);
        var total = subtotal - discount;
        var createdAt = DateTimeOffset.UtcNow;

        var receipt = new CheckoutReceipt
        {
            OrderId = order.Id,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            Items = order.Items
                .Select(item => new ReceiptLine
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    LineTotal = item.UnitPrice * item.Quantity
                })
                .ToList(),
            Subtotal = subtotal,
            Discount = discount,
            Total = total,
            CreatedAt = createdAt
        };

        await _orderRepository.SaveAsync(order, cancellationToken);
        await _notificationService.SendOrderConfirmationAsync(receipt, cancellationToken);

        return receipt;
    }

    private static void ValidateOrder(Order order)
    {
        if (order.Items is null || order.Items.Count == 0)
            throw new EmptyOrderException();

        foreach (var item in order.Items)
        {
            if (item.Quantity <= 0)
                throw new InvalidQuantityException(item.ProductName);

            if (item.UnitPrice < 0)
                throw new InvalidPriceException(item.ProductName);
        }
    }

    private async Task EnsureInventoryAsync(Order order, CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            var available = await _inventoryService.IsAvailableAsync(
                item.ProductId,
                item.Quantity,
                cancellationToken);

            if (!available)
                throw new InsufficientInventoryException(item.ProductName);
        }
    }

    private static decimal CalculateSubtotal(Order order) =>
        order.Items.Sum(item => item.UnitPrice * item.Quantity);

    internal static decimal CalculateDiscount(decimal subtotal)
    {
        if (subtotal < DiscountThreshold)
            return 0m;

        var discount = decimal.Round(subtotal * DiscountRate, 2, MidpointRounding.AwayFromZero);
        return discount > MaxDiscount ? MaxDiscount : discount;
    }
}
