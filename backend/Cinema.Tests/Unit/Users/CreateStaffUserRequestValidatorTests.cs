using Cinema.Application.Users;
using FluentAssertions;

namespace Cinema.Tests.Unit.Users;

public sealed class CreateStaffUserRequestValidatorTests
{
    [Theory]
    [InlineData("Admin")]
    [InlineData("Cashier")]
    public void Validate_AcceptsAllowedRoles(string role)
    {
        var validator = new CreateStaffUserRequestValidator();

        var result = validator.Validate(new CreateStaffUserRequest(
            "staff@example.com",
            "secret1",
            "Grace",
            "Hopper",
            role));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Manager")]
    [InlineData("admin")]
    public void Validate_RejectsUnsupportedRoles(string role)
    {
        var validator = new CreateStaffUserRequestValidator();

        var result = validator.Validate(new CreateStaffUserRequest(
            "staff@example.com",
            "secret1",
            "Grace",
            "Hopper",
            role));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    [Theory]
    [InlineData("", "secret1", "Grace", "Hopper", "Admin", "Email")]
    [InlineData("not-an-email", "secret1", "Grace", "Hopper", "Admin", "Email")]
    [InlineData("staff@example.com", "12345", "Grace", "Hopper", "Admin", "Password")]
    [InlineData("staff@example.com", "secret1", "", "Hopper", "Admin", "FirstName")]
    [InlineData("staff@example.com", "secret1", "Grace", "", "Admin", "LastName")]
    public void Validate_RejectsInvalidIdentityFields(
        string email,
        string password,
        string firstName,
        string lastName,
        string role,
        string property)
    {
        var validator = new CreateStaffUserRequestValidator();

        var result = validator.Validate(new CreateStaffUserRequest(email, password, firstName, lastName, role));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == property);
    }
}
