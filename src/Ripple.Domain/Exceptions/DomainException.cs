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
