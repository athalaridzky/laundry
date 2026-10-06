using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Domain.Exceptions;

namespace RefalinaLaundry.Web.Controllers;

public abstract class AppController : Controller
{
    protected int CurrentUserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0",
        System.Globalization.CultureInfo.InvariantCulture);

    protected bool CurrentUserIsOwner => User.IsInRole(RoleNames.Owner);

    protected void AddApplicationError(Exception exception)
    {
        switch (exception)
        {
            case ValidationException validationException:
                foreach (FluentValidation.Results.ValidationFailure error in validationException.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }

                break;
            case DomainRuleException or ConflictException or ForbiddenOperationException or EntityNotFoundException:
                ModelState.AddModelError(string.Empty, exception.Message);
                break;
            default:
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
                break;
        }
    }
}
