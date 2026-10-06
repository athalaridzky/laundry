namespace RefalinaLaundry.Domain.Enums;

public enum ServiceCategory : byte
{
    PerKilo = 0,
    PerItem = 1,
}

public enum OrderStatus : byte
{
    Received = 0,
    InProcess = 1,
    Done = 2,
    PickedUp = 3,
    Cancelled = 4,
}

public enum PaymentStatus : byte
{
    Unpaid = 0,
    PartiallyPaid = 1,
    Paid = 2,
}

public enum PaymentMethod : byte
{
    Cash = 0,
    Transfer = 1,
    Qris = 2,
}
