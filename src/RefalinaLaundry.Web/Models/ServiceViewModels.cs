using System.ComponentModel.DataAnnotations;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Enums;

namespace RefalinaLaundry.Web.Models;

public sealed class ServiceFormModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nama layanan wajib diisi.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Kategori")]
    public ServiceCategory Category { get; set; }

    [Range(1, 999_999_999, ErrorMessage = "Harga harus lebih dari nol.")]
    [Display(Name = "Harga satuan")]
    public decimal UnitPrice { get; set; }

    [Range(0, 365, ErrorMessage = "Durasi tidak boleh negatif.")]
    [Display(Name = "Durasi (hari)")]
    public int? DurationDays { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public ServiceInput ToInput() => new(Name, Category, UnitPrice, DurationDays, IsActive);
}

public sealed record ServicePageViewModel(IReadOnlyList<ServiceDto> Services, ServiceFormModel Form);
