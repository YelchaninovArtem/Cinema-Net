using FluentValidation;

namespace Cinema.Application.Users;

public sealed record StaffUserDto(string Id, string Email, string FirstName, string LastName, string Role);

public sealed record CreateStaffUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role);

public sealed class CreateStaffUserRequestValidator : AbstractValidator<CreateStaffUserRequest>
{
    private static readonly string[] AllowedRoles = ["Admin", "Cashier"];

    public CreateStaffUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).NotEmpty().Must(r => AllowedRoles.Contains(r))
            .WithMessage("Role must be 'Admin' or 'Cashier'.");
    }
}
