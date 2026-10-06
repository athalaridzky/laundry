using Microsoft.AspNetCore.Identity;

namespace RefalinaLaundry.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; }
}
