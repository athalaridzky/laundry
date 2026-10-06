using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Enums;
using RefalinaLaundry.Domain.Exceptions;
using RefalinaLaundry.Web.Models;

namespace RefalinaLaundry.Web.Controllers;

[Authorize(Roles = RoleNames.Operational)]
[Route("Orders")]
public sealed class OrdersController : AppController
{
    private readonly IOrderService _orders;
    private readonly IServiceCatalogService _services;
    private readonly IBusinessSettingService _settings;
    private readonly IBusinessTimeZone _businessTime;

    public OrdersController(
        IOrderService orders,
        IServiceCatalogService services,
        IBusinessSettingService settings,
        IBusinessTimeZone businessTime)
    {
        _orders = orders;
        _services = services;
        _settings = settings;
        _businessTime = businessTime;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search,
        DateOnly? from,
        DateOnly? to,
        OrderStatus? status,
        PaymentStatus? paymentStatus,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        DateTime? fromUtc = from.HasValue
            ? _businessTime.ToUtc(DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified))
            : null;
        DateTime? toUtc = to.HasValue
            ? _businessTime.ToUtc(DateTime.SpecifyKind(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified))
            : null;
        PagedResult<OrderListItemDto> orders = await _orders.SearchAsync(
            new OrderSearchCriteria(search, fromUtc, toUtc, status, paymentStatus, page),
            cancellationToken);
        return View(new OrderIndexViewModel(orders, search, from, to, status, paymentStatus));
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        CreateOrderPageViewModel page = await BuildCreatePageAsync(new CreateOrderInputModel(), cancellationToken);
        return View(page);
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Form")] CreateOrderInputModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildCreatePageAsync(form, cancellationToken));
        }

        try
        {
            CreateOrderResult result = await _orders.CreateAsync(form.ToCommand(), CurrentUserId, cancellationToken);
            TempData["Success"] = $"Transaksi {result.OrderNumber} berhasil disimpan.";
            return RedirectToAction(nameof(Receipt), new
            {
                id = result.Id,
                print = true,
                change = result.ChangeAmount.ToString(CultureInfo.InvariantCulture),
            });
        }
        catch (Exception exception) when (exception is ValidationException or DomainRuleException or ConflictException or EntityNotFoundException)
        {
            AddApplicationError(exception);
            return View(await BuildCreatePageAsync(form, cancellationToken));
        }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        OrderDetailDto order = await _orders.GetAsync(id, cancellationToken);
        return View(new OrderDetailPageViewModel(order, new PaymentFormModel { Amount = order.OutstandingAmount }));
    }

    [HttpPost("{id:long}/Status")]
    public async Task<IActionResult> Status(
        long id,
        OrderStatus status,
        string? note,
        CancellationToken cancellationToken)
    {
        try
        {
            await _orders.ChangeStatusAsync(new ChangeOrderStatusCommand(id, status, note), CurrentUserId, cancellationToken);
            TempData["Success"] = "Status pesanan diperbarui.";
        }
        catch (DomainRuleException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:long}/Cancel")]
    public async Task<IActionResult> Cancel(long id, string reason, CancellationToken cancellationToken)
    {
        try
        {
            await _orders.CancelAsync(new CancelOrderCommand(id, reason), CurrentUserId, CurrentUserIsOwner, cancellationToken);
            TempData["Success"] = "Pesanan dibatalkan dan dicatat dalam audit.";
        }
        catch (DomainRuleException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:long}/Payments")]
    public async Task<IActionResult> Payment(
        long id,
        [Bind(Prefix = "Payment")] PaymentFormModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Periksa kembali data pembayaran.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            PaymentResult result = await _orders.AddPaymentAsync(
                new AddPaymentCommand(id, form.Amount, form.Method, form.CashTendered, form.Reference),
                CurrentUserId,
                cancellationToken);
            TempData["Success"] = result.ChangeAmount > 0
                ? $"Pembayaran dicatat. Kembalian {Web.Common.UiFormat.Currency(result.ChangeAmount)}."
                : "Pembayaran berhasil dicatat.";
        }
        catch (Exception exception) when (exception is ValidationException or DomainRuleException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("{id:long}/Receipt")]
    public async Task<IActionResult> Receipt(
        long id,
        bool print = false,
        bool copy = false,
        decimal change = 0m,
        CancellationToken cancellationToken = default)
    {
        OrderDetailDto order = await _orders.GetAsync(id, cancellationToken);
        BusinessSettingDto settings = await _settings.GetAsync(cancellationToken);
        ViewData["AutoPrint"] = print;
        return View(new ReceiptViewModel(order, settings, copy, change));
    }

    [HttpPost("{id:long}/Receipt/Reprint")]
    public async Task<IActionResult> ReprintReceipt(long id, CancellationToken cancellationToken)
    {
        await _orders.MarkReceiptReprintedAsync(id, CurrentUserId, cancellationToken);
        return RedirectToAction(nameof(Receipt), new { id, print = true, copy = true });
    }

    private async Task<CreateOrderPageViewModel> BuildCreatePageAsync(
        CreateOrderInputModel form,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ServiceDto> services = await _services.ListAsync(true, cancellationToken);
        return new CreateOrderPageViewModel(form, services);
    }
}
