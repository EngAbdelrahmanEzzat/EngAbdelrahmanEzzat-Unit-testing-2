using System.Collections.Concurrent;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;

namespace Simulation.Checkout.Infrastructure;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public Task SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        cancellationToken.ThrowIfCancellationRequested();

        _orders[order.Id] = order;
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<Order> GetAll() => _orders.Values.ToList();
}
