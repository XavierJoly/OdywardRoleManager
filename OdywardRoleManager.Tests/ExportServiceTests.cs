using _0900_OdywardRoleManager.Models;
using _0900_OdywardRoleManager.Services;

namespace OdywardRoleManager.Tests;

public class ExportServiceTests : IAsyncLifetime
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private ExportService _service = null!;

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_rootDirectory);
        _service = new ExportService(_rootDirectory);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task SaveAuditEntryAsync_AppendsEntries()
    {
        var entry = new AuditEntry
        {
            Email = "user@example.com",
            TenantId = "tenant",
            ActorUpn = "actor@example.com",
            RolesBefore = new[] { "RoleA" },
            RolesAfter = new[] { "RoleA", "RoleB" },
            Timestamp = DateTimeOffset.UtcNow,
        };

        await _service.SaveAuditEntryAsync(entry, CancellationToken.None);

        var entries = await _service.ReadAuditEntriesForUserAsync(entry.Email, CancellationToken.None);

        Assert.Single(entries);
        Assert.Equal(entry.Email, entries[0].Email);
        Assert.Contains("RoleB", entries[0].RolesAfter);
    }

    [Fact]
    public async Task ExportAssignmentReportAsync_CreatesFile()
    {
        var entry = new AuditEntry
        {
            Email = "user@example.com",
            TenantId = "tenant",
            ActorUpn = "actor@example.com",
            RolesBefore = Array.Empty<string>(),
            RolesAfter = new[] { "RoleA" },
            Timestamp = DateTimeOffset.UtcNow,
        };

        var path = await _service.ExportAssignmentReportAsync(entry, CancellationToken.None);

        Assert.True(File.Exists(path));
        var content = await File.ReadAllTextAsync(path);
        Assert.Contains("RoleA", content);
    }
}
