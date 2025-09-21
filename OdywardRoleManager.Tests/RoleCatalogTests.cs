using _0900_OdywardRoleManager.Services;

namespace OdywardRoleManager.Tests;

public class RoleCatalogTests
{
    private readonly RoleCatalog _catalog = new();

    [Fact]
    public void CreateRoleModels_ReturnsAllSupportedRoles()
    {
        var roles = _catalog.CreateRoleModels().ToList();

        Assert.Equal(3, roles.Count);
        Assert.Contains(roles, r => r.DisplayName == "Application Administrator");
        Assert.All(roles, role => Assert.False(string.IsNullOrWhiteSpace(role.TemplateId)));
    }

    [Fact]
    public void TryGetDefinition_ReturnsTemplateId()
    {
        var success = _catalog.TryGetDefinition("User Administrator", out var definition);

        Assert.True(success);
        Assert.NotNull(definition);
        Assert.Equal("fe930be7-5e62-47db-91af-98c3a49a38b1", definition!.RoleTemplateId);
    }

    [Fact]
    public void TryGetDefinition_ReturnsFalse_ForUnknownRole()
    {
        var success = _catalog.TryGetDefinition("Unknown", out var definition);

        Assert.False(success);
        Assert.Null(definition);
    }
}
