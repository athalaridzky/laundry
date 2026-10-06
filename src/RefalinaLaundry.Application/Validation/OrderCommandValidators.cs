using FluentValidation;
using RefalinaLaundry.Application.Contracts;

namespace RefalinaLaundry.Application.Validation;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.CustomerId)
            .GreaterThan(0).WithMessage("Pelanggan wajib dipilih.");
        RuleFor(command => command.Items)
            .NotEmpty().WithMessage("Tambahkan minimal satu layanan.")
            .Must(items => items.Select(item => item.ServiceId).Distinct().Count() == items.Count)
            .WithMessage("Layanan yang sama tidak boleh ditambahkan dua kali.");
        RuleForEach(command => command.Items).ChildRules(item =>
        {
            item.RuleFor(value => value.ServiceId)
                .GreaterThan(0).WithMessage("Layanan wajib dipilih.");
            item.RuleFor(value => value.Quantity)
                .GreaterThan(0).WithMessage("Berat atau jumlah harus lebih dari nol.");
        });
        RuleFor(command => command.Notes)
            .MaximumLength(500).WithMessage("Catatan maksimal 500 karakter.");
        When(command => command.InitialPayment is not null, () =>
        {
            RuleFor(command => command.InitialPayment!.Amount)
                .GreaterThan(0).WithMessage("Jumlah pembayaran harus lebih dari nol.");
            RuleFor(command => command.InitialPayment!.Method)
                .IsInEnum().WithMessage("Metode pembayaran tidak valid.");
            RuleFor(command => command.InitialPayment!.Reference)
                .MaximumLength(100).WithMessage("Referensi maksimal 100 karakter.");
        });
    }
}

public sealed class AddPaymentCommandValidator : AbstractValidator<AddPaymentCommand>
{
    public AddPaymentCommandValidator()
    {
        RuleFor(command => command.OrderId).GreaterThan(0);
        RuleFor(command => command.Amount)
            .GreaterThan(0).WithMessage("Jumlah pembayaran harus lebih dari nol.");
        RuleFor(command => command.Method)
            .IsInEnum().WithMessage("Metode pembayaran tidak valid.");
        RuleFor(command => command.Reference)
            .MaximumLength(100).WithMessage("Referensi maksimal 100 karakter.");
    }
}

public sealed class BusinessSettingInputValidator : AbstractValidator<BusinessSettingInput>
{
    public BusinessSettingInputValidator()
    {
        RuleFor(input => input.BusinessName).NotEmpty().MaximumLength(150).WithMessage("Nama usaha wajib diisi, maksimal 150 karakter.");
        RuleFor(input => input.Address).NotEmpty().MaximumLength(300).WithMessage("Alamat wajib diisi, maksimal 300 karakter.");
        RuleFor(input => input.PhoneNumber).MaximumLength(20);
        RuleFor(input => input.ReceiptFooter).MaximumLength(500);
        RuleFor(input => input.PaperWidthMm).Must(value => value is 58 or 80).WithMessage("Lebar kertas harus 58 atau 80 mm.");
        RuleFor(input => input.OrderNumberPrefix).NotEmpty().MaximumLength(10).Matches("^[A-Za-z0-9]+$").WithMessage("Prefix nota wajib berupa huruf/angka, maksimal 10 karakter.");
        RuleFor(input => input.MaxWeightKg).GreaterThan(0).LessThanOrEqualTo(999_999m).WithMessage("Batas berat harus lebih dari nol.");
    }
}

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.UserName).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9._-]+$").WithMessage("Username hanya boleh berisi huruf, angka, titik, garis bawah, atau tanda hubung.");
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Role).Must(role => role is Common.RoleNames.Owner or Common.RoleNames.Cashier).WithMessage("Peran pengguna tidak valid.");
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8).WithMessage("Password minimal 8 karakter.");
    }
}
