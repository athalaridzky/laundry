using RefalinaLaundry.Domain.Enums;

namespace RefalinaLaundry.Application.Contracts;

public sealed record ServiceInput(
    string Name,
    ServiceCategory Category,
    decimal UnitPrice,
    int? DurationDays,
    bool IsActive);

public sealed record ServiceDto(
    int Id,
    string Name,
    ServiceCategory Category,
    decimal UnitPrice,
    int? DurationDays,
    bool IsActive);
