using RefalinaLaundry.Domain.Exceptions;

namespace RefalinaLaundry.Domain.Entities;

public sealed class OrderCounter
{
    private OrderCounter()
    {
    }

    public OrderCounter(DateOnly date)
    {
        Date = date;
    }

    public DateOnly Date { get; private set; }

    public int LastNumber { get; private set; }

    public int Next()
    {
        if (LastNumber >= 9_999)
        {
            throw new DomainRuleException("order.counter_limit", "Batas nomor nota harian telah tercapai.");
        }

        LastNumber++;
        return LastNumber;
    }
}
