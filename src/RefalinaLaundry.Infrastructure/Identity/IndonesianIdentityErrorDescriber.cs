using Microsoft.AspNetCore.Identity;

namespace RefalinaLaundry.Infrastructure.Identity;

public sealed class IndonesianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Error("DefaultError", "Permintaan akun tidak dapat diproses.");

    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "Password lama tidak sesuai.");

    public override IdentityError InvalidUserName(string? userName) =>
        Error(nameof(InvalidUserName), $"Username '{userName}' tidak valid.");

    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), $"Username '{userName}' sudah digunakan.");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), $"Password minimal {length} karakter.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric), "Password harus memiliki minimal satu simbol.");

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "Password harus memiliki minimal satu angka.");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "Password harus memiliki minimal satu huruf kecil.");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "Password harus memiliki minimal satu huruf besar.");

    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };
}
