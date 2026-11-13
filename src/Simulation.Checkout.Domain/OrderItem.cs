namespace Simulation.Checkout.Domain;

public sealed class OrderItem
{
    public required Guid ProductId { get; init; }

    public required string ProductName { get; init; }

    public required decimal UnitPrice { get; init; }

    public required int Quantity { get; init; }
}
