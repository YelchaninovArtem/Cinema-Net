using Cinema.Application.Auth;
using FluentAssertions;

namespace Cinema.Tests.Unit.Auth;

public sealed class AuthValidatorTests
{
    [Fact]
    public void LoginRequestValidator_AcceptsValidRequest()
    {
        var validator = new LoginRequestValidator();

        var result = validator.Validate(new LoginRequest("user@example.com", "password"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "password", "Email")]
    [InlineData("not-an-email", "password", "Email")]
    [InlineData("user@example.com", "", "Password")]
    public void LoginRequestValidator_RejectsInvalidRequest(string email, string password, string property)
    {
        var validator = new LoginRequestValidator();

        var result = validator.Validate(new LoginRequest(email, password));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == property);
    }

    [Fact]
    public void RegisterRequestValidator_AcceptsValidRequest()
    {
        var validator = new RegisterRequestValidator();

        var result = validator.Validate(new RegisterRequest(
            "user@example.com",
            "secret1",
            "Ada",
            "Lovelace"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "secret1", "Ada", "Lovelace", "Email")]
    [InlineData("not-an-email", "secret1", "Ada", "Lovelace", "Email")]
    [InlineData("user@example.com", "12345", "Ada", "Lovelace", "Password")]
    [InlineData("user@example.com", "secret1", "", "Lovelace", "FirstName")]
    [InlineData("user@example.com", "secret1", "Ada", "", "LastName")]
    public void RegisterRequestValidator_RejectsInvalidRequest(
        string email,
        string password,
        string firstName,
        string lastName,
        string property)
    {
        var validator = new RegisterRequestValidator();

        var result = validator.Validate(new RegisterRequest(email, password, firstName, lastName));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == property);
    }

    [Fact]
    public void RegisterRequestValidator_RejectsTooLongFields()
    {
        var validator = new RegisterRequestValidator();

        var result = validator.Validate(new RegisterRequest(
            $"{new string('a', 257)}@example.com",
            new string('p', 101),
            new string('f', 101),
            new string('l', 101)));

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.PropertyName).Should().Contain(["Email", "Password", "FirstName", "LastName"]);
    }

    [Fact]
    public void RefreshRequestValidator_AcceptsNonEmptyToken()
    {
        var validator = new RefreshRequestValidator();

        var result = validator.Validate(new RefreshRequest("refresh-token"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RefreshRequestValidator_RejectsEmptyToken()
    {
        var validator = new RefreshRequestValidator();

        var result = validator.Validate(new RefreshRequest(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RefreshToken");
    }
}
