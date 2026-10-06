namespace RefalinaLaundry.Application.Contracts;

public sealed record CustomerInput(string Name, string? PhoneNumber, string? Address, string? Notes);

public sealed record CustomerDto(
    int Id,
    string Name,
    string? PhoneNumber,
    string? Address,
    string? Notes,
    int OrderCount = 0);
