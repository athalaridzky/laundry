using RefalinaLaundry.Domain.Exceptions;

namespace RefalinaLaundry.Domain.Entities;

public sealed class BusinessSetting
{
    private BusinessSetting()
    {
    }

    public BusinessSetting(
        string businessName,
        string address,
        string? phoneNumber,
        string? receiptFooter,
        int paperWidthMm,
        string orderNumberPrefix,
        decimal maxWeightKg)
    {
        Update(businessName, address, phoneNumber, receiptFooter, paperWidthMm, orderNumberPrefix, maxWeightKg);
    }

    public int Id { get; private set; }

    public string BusinessName { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public string? ReceiptFooter { get; private set; }

    public int PaperWidthMm { get; private set; }

    public string OrderNumberPrefix { get; private set; } = string.Empty;

    public decimal MaxWeightKg { get; private set; }

    public void Update(
        string businessName,
        string address,
        string? phoneNumber,
        string? receiptFooter,
        int paperWidthMm,
        string orderNumberPrefix,
        decimal maxWeightKg)
    {
        if (string.IsNullOrWhiteSpace(businessName) || string.IsNullOrWhiteSpace(address))
        {
            throw new DomainRuleException("settings.required", "Nama dan alamat usaha wajib diisi.");
        }

        if (paperWidthMm is not (58 or 80))
        {
            throw new DomainRuleException("settings.paper", "Lebar kertas harus 58 mm atau 80 mm.");
        }

        if (string.IsNullOrWhiteSpace(orderNumberPrefix) || maxWeightKg <= 0)
        {
            throw new DomainRuleException("settings.value", "Prefix nota dan batas berat harus valid.");
        }

        BusinessName = businessName.Trim();
        Address = address.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        ReceiptFooter = string.IsNullOrWhiteSpace(receiptFooter) ? null : receiptFooter.Trim();
        PaperWidthMm = paperWidthMm;
        OrderNumberPrefix = orderNumberPrefix.Trim().ToUpperInvariant();
        MaxWeightKg = maxWeightKg;
    }
}
