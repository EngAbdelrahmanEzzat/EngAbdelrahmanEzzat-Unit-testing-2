using Simulation.Checkout.Domain;

namespace Simulation.Checkout.Application.Abstractions;

public interface IOrderRepository
{
    Task SaveAsync(Order order, CancellationToken cancellationToken = default);
}
