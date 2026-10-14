namespace Ripple.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class EventNotFoundException : DomainException
{
    public EventNotFoundException(Guid id) : base($"Event '{id}' was not found.") { }
}

public class PricingTierNotFoundException : DomainException
{
    public PricingTierNotFoundException(Guid id) : base($"Pricing tier '{id}' was not found.") { }
}

/// <summary>
/// A change that would remove or invalidate tickets already sold: deleting an event or pricing tier
/// that has sales, or reducing a tier's capacity below what has been sold.
/// </summary>
public class SalesHistoryConflictException : DomainException
{
    public SalesHistoryConflictException(string message) : base(message) { }
}

public class InsufficientCapacityException : DomainException
{
    public int Available { get; }
    public int Requested { get; }

    public InsufficientCapacityException(int available, int requested)
        : base($"Insufficient capacity. Available: {available}, Requested: {requested}.")
    {
        Available = available;
        Requested = requested;
    }
}
