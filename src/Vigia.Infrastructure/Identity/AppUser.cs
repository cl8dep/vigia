using Microsoft.AspNetCore.Identity;

namespace Vigia.Infrastructure.Identity;

/// <summary>
/// Application user managed by ASP.NET Core Identity.
/// </summary>
public sealed class AppUser : IdentityUser<Guid>;
