using Simulation.Checkout.Domain;

namespace Simulation.Checkout.Application.Abstractions;

public interface INotificationService
{
    Task SendOrderConfirmationAsync(
        CheckoutReceipt receipt,
        CancellationToken cancellationToken = default);
}
