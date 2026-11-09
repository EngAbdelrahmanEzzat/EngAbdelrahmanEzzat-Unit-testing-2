namespace Simulation.Checkout.Domain.Exceptions;

public class CheckoutException : Exception
{
    public CheckoutException(string message)
        : base(message)
    {
    }
}

public sealed class EmptyOrderException : CheckoutException
{
    public EmptyOrderException()
        : base("An order must contain at least one item.")
    {
    }
}

public sealed class InvalidQuantityException : CheckoutException
{
    public InvalidQuantityException(string productName)
        : base($"Quantity for '{productName}' must be greater than zero.")
    {
    }
}

public sealed class InvalidPriceException : CheckoutException
{
    public InvalidPriceException(string productName)
        : base($"Unit price for '{productName}' cannot be negative.")
    {
    }
}

public sealed class InsufficientInventoryException : CheckoutException
{
    public InsufficientInventoryException(string productName)
        : base($"Insufficient inventory for '{productName}'.")
    {
    }
}
