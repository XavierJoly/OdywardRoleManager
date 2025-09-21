using _0900_OdywardRoleManager.Utils;

namespace OdywardRoleManager.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@sub.domain.fr")]
    [InlineData("user+alias@domain.io")]
    public void IsValidEmail_ReturnsTrue_ForValidAddresses(string email)
    {
        Assert.True(Validation.IsValidEmail(email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("user@domain")]
    [InlineData("user@@domain.com")]
    public void IsValidEmail_ReturnsFalse_ForInvalidAddresses(string? email)
    {
        Assert.False(Validation.IsValidEmail(email));
    }
}
