using System.ComponentModel.DataAnnotations;
using RefalinaLaundry.Application.Contracts;

namespace RefalinaLaundry.Web.Models;

public sealed class CustomerFormModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nama pelanggan wajib diisi.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Nomor HP")]
    public string? PhoneNumber { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public CustomerInput ToInput() => new(Name, PhoneNumber, Address, Notes);
}

public sealed record CustomerPageViewModel(IReadOnlyList<CustomerDto> Customers, string? Search, CustomerFormModel Form);
