using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Exceptions;
using RefalinaLaundry.Web.Models;

namespace RefalinaLaundry.Web.Controllers;

[Authorize(Roles = RoleNames.Operational)]
[Route("Services")]
public sealed class ServicesController : AppController
{
    private readonly IServiceCatalogService _services;

    public ServicesController(IServiceCatalogService services)
    {
        _services = services;
    }

    [Authorize(Roles = RoleNames.Owner)]
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        IReadOnlyList<ServiceDto> services = await _services.ListAsync(false, cancellationToken);
        return View(new ServicePageViewModel(services, new ServiceFormModel()));
    }

    [Authorize(Roles = RoleNames.Operational)]
    [HttpGet("Active")]
    public async Task<IActionResult> Active(CancellationToken cancellationToken) =>
        Json(await _services.ListAsync(true, cancellationToken));

    [Authorize(Roles = RoleNames.Owner)]
    [HttpPost("")]
    public async Task<IActionResult> Create(ServiceFormModel form, CancellationToken cancellationToken)
    {
        try
        {
            await _services.CreateAsync(form.ToInput(), CurrentUserId, cancellationToken);
            TempData["Success"] = "Layanan berhasil ditambahkan.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception) when (exception is ValidationException or DomainRuleException or ConflictException)
        {
            AddApplicationError(exception);
            IReadOnlyList<ServiceDto> services = await _services.ListAsync(false, cancellationToken);
            return View(nameof(Index), new ServicePageViewModel(services, form));
        }
    }

    [Authorize(Roles = RoleNames.Owner)]
    [HttpPost("{id:int}/Update")]
    public async Task<IActionResult> Update(int id, ServiceFormModel form, CancellationToken cancellationToken)
    {
        try
        {
            await _services.UpdateAsync(id, form.ToInput(), CurrentUserId, cancellationToken);
            TempData["Success"] = "Layanan berhasil diperbarui.";
        }
        catch (Exception exception) when (exception is ValidationException or DomainRuleException or ConflictException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Owner)]
    [HttpPost("{id:int}/Delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _services.DeleteAsync(id, CurrentUserId, cancellationToken);
        TempData["Success"] = "Layanan dinonaktifkan.";
        return RedirectToAction(nameof(Index));
    }
}
