using System.ComponentModel.DataAnnotations;
using RefalinaLaundry.Application.Contracts;

namespace RefalinaLaundry.Web.Models;

public sealed class BusinessSettingFormModel
{
    [Required]
    [StringLength(150)]
    public string BusinessName { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    public string? ReceiptFooter { get; set; }

    public int PaperWidthMm { get; set; } = 80;

    [Required]
    [StringLength(10)]
    public string OrderNumberPrefix { get; set; } = "RL";

    [Range(typeof(decimal), "0.1", "999999", ParseLimitsInInvariantCulture = true)]
    public decimal MaxWeightKg { get; set; } = 100m;

    public BusinessSettingInput ToInput() => new(
        BusinessName,
        Address,
        PhoneNumber,
        ReceiptFooter,
        PaperWidthMm,
        OrderNumberPrefix,
        MaxWeightKg);

    public static BusinessSettingFormModel FromDto(BusinessSettingDto setting) => new()
    {
        BusinessName = setting.BusinessName,
        Address = setting.Address,
        PhoneNumber = setting.PhoneNumber,
        ReceiptFooter = setting.ReceiptFooter,
        PaperWidthMm = setting.PaperWidthMm,
        OrderNumberPrefix = setting.OrderNumberPrefix,
        MaxWeightKg = setting.MaxWeightKg,
    };
}

public sealed class CreateUserFormModel
{
    [Required]
    [StringLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Cashier";

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public CreateUserCommand ToCommand() => new(UserName, FullName, Role, Password);
}

public sealed record SettingsPageViewModel(
    BusinessSettingFormModel Business,
    IReadOnlyList<UserAccountDto> Users,
    CreateUserFormModel NewUser);
