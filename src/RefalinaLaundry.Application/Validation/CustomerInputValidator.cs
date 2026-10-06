using FluentValidation;
using RefalinaLaundry.Application.Contracts;

namespace RefalinaLaundry.Application.Validation;

public sealed class CustomerInputValidator : AbstractValidator<CustomerInput>
{
    public CustomerInputValidator()
    {
        RuleFor(input => input.Name)
            .NotEmpty().WithMessage("Nama pelanggan wajib diisi.")
            .MaximumLength(150).WithMessage("Nama pelanggan maksimal 150 karakter.");
        RuleFor(input => input.PhoneNumber)
            .MaximumLength(20).WithMessage("Nomor HP maksimal 20 karakter.");
        RuleFor(input => input.Address)
            .MaximumLength(300).WithMessage("Alamat maksimal 300 karakter.");
        RuleFor(input => input.Notes)
            .MaximumLength(500).WithMessage("Catatan maksimal 500 karakter.");
    }
}
