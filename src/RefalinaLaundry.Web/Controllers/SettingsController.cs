using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Exceptions;
using RefalinaLaundry.Web.Models;

namespace RefalinaLaundry.Web.Controllers;

[Authorize(Roles = RoleNames.Owner)]
[Route("Settings")]
public sealed class SettingsController : AppController
{
    private readonly IBusinessSettingService _settings;
    private readonly IUserAccountService _users;

    public SettingsController(IBusinessSettingService settings, IUserAccountService users)
    {
        _settings = settings;
        _users = users;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        SettingsPageViewModel page = await BuildPageAsync(cancellationToken);
        return View(page);
    }

    [HttpPost("Business")]
    public async Task<IActionResult> UpdateBusiness(BusinessSettingFormModel form, CancellationToken cancellationToken)
    {
        try
        {
            await _settings.UpdateAsync(form.ToInput(), CurrentUserId, cancellationToken);
            TempData["Success"] = "Pengaturan usaha diperbarui.";
        }
        catch (Exception exception) when (exception is ValidationException or DomainRuleException or ConflictException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Users")]
    public async Task<IActionResult> CreateUser(CreateUserFormModel form, CancellationToken cancellationToken)
    {
        try
        {
            await _users.CreateAsync(form.ToCommand(), cancellationToken);
            TempData["Success"] = "Akun pengguna berhasil dibuat.";
        }
        catch (ConflictException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Users/{id:int}/Active")]
    public async Task<IActionResult> SetUserActive(int id, bool isActive, CancellationToken cancellationToken)
    {
        if (id == CurrentUserId && !isActive)
        {
            TempData["Error"] = "Akun yang sedang digunakan tidak dapat dinonaktifkan.";
            return RedirectToAction(nameof(Index));
        }

        await _users.SetActiveAsync(id, isActive, cancellationToken);
        TempData["Success"] = isActive ? "Akun diaktifkan." : "Akun dinonaktifkan.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SettingsPageViewModel> BuildPageAsync(CancellationToken cancellationToken)
    {
        BusinessSettingDto business = await _settings.GetAsync(cancellationToken);
        IReadOnlyList<UserAccountDto> users = await _users.ListAsync(cancellationToken);
        return new SettingsPageViewModel(
            BusinessSettingFormModel.FromDto(business),
            users,
            new CreateUserFormModel());
    }
}
