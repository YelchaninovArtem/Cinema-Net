using Cinema.Application.Users;
using Cinema.Domain.Common;
using Cinema.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Admin;

public sealed class StaffUserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : IStaffUserService
{
    public async Task<IReadOnlyList<StaffUserDto>> GetStaffAsync(CancellationToken ct = default)
    {
        var admins = await userManager.GetUsersInRoleAsync("Admin");
        var cashiers = await userManager.GetUsersInRoleAsync("Cashier");

        return admins.Select(u => new StaffUserDto(u.Id, u.Email!, u.FirstName, u.LastName, "Admin"))
            .Concat(cashiers.Select(u => new StaffUserDto(u.Id, u.Email!, u.FirstName, u.LastName, "Cashier")))
            .OrderBy(u => u.Role).ThenBy(u => u.Email)
            .ToList();
    }

    public async Task<StaffUserDto> CreateAsync(CreateStaffUserRequest request, CancellationToken ct = default)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new DomainException($"User with email '{request.Email}' already exists.");

        if (!await roleManager.RoleExistsAsync(request.Role))
            throw new DomainException($"Role '{request.Role}' does not exist.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new DomainException(errors);
        }

        await userManager.AddToRoleAsync(user, request.Role);
        return new StaffUserDto(user.Id, user.Email!, user.FirstName, user.LastName, request.Role);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id)
            ?? throw new DomainException($"User with ID '{id}' not found.");

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new DomainException(errors);
        }
    }
}
