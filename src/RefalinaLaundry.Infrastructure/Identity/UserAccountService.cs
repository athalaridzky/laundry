using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;

namespace RefalinaLaundry.Infrastructure.Identity;

public sealed class UserAccountService : IUserAccountService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;

    public UserAccountService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<int>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<UserAccountDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        List<AppUser> users = await _userManager.Users.AsNoTracking().OrderBy(user => user.FullName).ToListAsync(cancellationToken);
        List<UserAccountDto> result = new(users.Count);
        foreach (AppUser user in users)
        {
            IList<string> roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserAccountDto(
                user.Id,
                user.UserName ?? string.Empty,
                user.FullName,
                roles.FirstOrDefault() ?? string.Empty,
                user.IsActive));
        }

        return result;
    }

    public async Task CreateAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (command.Role is not (RoleNames.Owner or RoleNames.Cashier) || !await _roleManager.RoleExistsAsync(command.Role))
        {
            throw new ConflictException("Peran pengguna tidak valid.");
        }

        if (await _userManager.FindByNameAsync(command.UserName) is not null)
        {
            throw new ConflictException("Username sudah digunakan.");
        }

        AppUser user = new()
        {
            UserName = command.UserName.Trim(),
            FullName = command.FullName.Trim(),
            IsActive = true,
            MustChangePassword = true,
            LockoutEnabled = true,
        };
        IdentityResult creation = await _userManager.CreateAsync(user, command.Password);
        EnsureSucceeded(creation);
        IdentityResult roleAssignment = await _userManager.AddToRoleAsync(user, command.Role);
        if (!roleAssignment.Succeeded)
        {
            _ = await _userManager.DeleteAsync(user);
            EnsureSucceeded(roleAssignment);
        }
    }

    public async Task SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AppUser user = await _userManager.FindByIdAsync(userId.ToString(System.Globalization.CultureInfo.InvariantCulture))
            ?? throw new EntityNotFoundException("Pengguna tidak ditemukan.");
        user.IsActive = isActive;
        IdentityResult result = await _userManager.UpdateAsync(user);
        EnsureSucceeded(result);
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            string message = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new ConflictException(message);
        }
    }
}
