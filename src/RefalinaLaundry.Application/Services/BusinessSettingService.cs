using FluentValidation;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Entities;

namespace RefalinaLaundry.Application.Services;

public sealed class BusinessSettingService : IBusinessSettingService
{
    private readonly IBusinessSettingRepository _settings;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<BusinessSettingInput> _validator;

    public BusinessSettingService(
        IBusinessSettingRepository settings,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<BusinessSettingInput> validator)
    {
        _settings = settings;
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<BusinessSettingDto> GetAsync(CancellationToken cancellationToken = default)
    {
        BusinessSetting setting = await _settings.GetAsync(cancellationToken)
            ?? throw new EntityNotFoundException("Pengaturan usaha belum tersedia.");
        return Map(setting);
    }

    public async Task UpdateAsync(
        BusinessSettingInput input,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(input, cancellationToken);
        BusinessSetting setting = await _settings.GetAsync(cancellationToken)
            ?? throw new EntityNotFoundException("Pengaturan usaha belum tersedia.");
        setting.Update(
            input.BusinessName,
            input.Address,
            input.PhoneNumber,
            input.ReceiptFooter,
            input.PaperWidthMm,
            input.OrderNumberPrefix,
            input.MaxWeightKg);
        _auditLogs.Add(new AuditLog(
            userId,
            AuditActions.BusinessSettingsChanged,
            nameof(BusinessSetting),
            setting.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "Pengaturan usaha diperbarui.",
            _clock.UtcNow));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static BusinessSettingDto Map(BusinessSetting setting) =>
        new(
            setting.Id,
            setting.BusinessName,
            setting.Address,
            setting.PhoneNumber,
            setting.ReceiptFooter,
            setting.PaperWidthMm,
            setting.OrderNumberPrefix,
            setting.MaxWeightKg);
}
