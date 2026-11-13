namespace Simulation.Checkout.Domain;

public sealed class CheckoutReceipt
{
    public required Guid OrderId { get; init; }

    public required string CustomerName { get; init; }

    public required string CustomerEmail { get; init; }

    public required IReadOnlyList<ReceiptLine> Items { get; init; }

    public required decimal Subtotal { get; init; }

    public required decimal Discount { get; init; }

    public required decimal Total { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class ReceiptLine
{
    public required Guid ProductId { get; init; }

    public required string ProductName { get; init; }

    public required decimal UnitPrice { get; init; }

    public required int Quantity { get; init; }

    public required decimal LineTotal { get; init; }
}
