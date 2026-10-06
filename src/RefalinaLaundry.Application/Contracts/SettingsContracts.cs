namespace RefalinaLaundry.Application.Contracts;

public sealed record BusinessSettingDto(
    int Id,
    string BusinessName,
    string Address,
    string? PhoneNumber,
    string? ReceiptFooter,
    int PaperWidthMm,
    string OrderNumberPrefix,
    decimal MaxWeightKg);

public sealed record BusinessSettingInput(
    string BusinessName,
    string Address,
    string? PhoneNumber,
    string? ReceiptFooter,
    int PaperWidthMm,
    string OrderNumberPrefix,
    decimal MaxWeightKg);

public sealed record UserAccountDto(int Id, string UserName, string FullName, string Role, bool IsActive);

public sealed record CreateUserCommand(string UserName, string FullName, string Role, string Password);
