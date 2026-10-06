using FluentValidation;
using RefalinaLaundry.Application.Contracts;

namespace RefalinaLaundry.Application.Validation;

public sealed class ServiceInputValidator : AbstractValidator<ServiceInput>
{
    public ServiceInputValidator()
    {
        RuleFor(input => input.Name)
            .NotEmpty().WithMessage("Nama layanan wajib diisi.")
            .MaximumLength(100).WithMessage("Nama layanan maksimal 100 karakter.");
        RuleFor(input => input.Category)
            .IsInEnum().WithMessage("Kategori layanan tidak valid.");
        RuleFor(input => input.UnitPrice)
            .GreaterThan(0).WithMessage("Harga harus lebih dari nol.");
        RuleFor(input => input.DurationDays)
            .GreaterThanOrEqualTo(0).When(input => input.DurationDays.HasValue)
            .WithMessage("Durasi tidak boleh negatif.");
    }
}
