using System.Security.Claims;

namespace Cinema.Api;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
        => user.FindFirstValue("sub")
           ?? throw new InvalidOperationException("User ID claim not found.");
}
