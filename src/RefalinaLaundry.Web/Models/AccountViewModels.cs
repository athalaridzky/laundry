using System.ComponentModel.DataAnnotations;

namespace RefalinaLaundry.Web.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Username wajib diisi.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password wajib diisi.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Password lama wajib diisi.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password baru wajib diisi.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword), ErrorMessage = "Konfirmasi password tidak sama.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
