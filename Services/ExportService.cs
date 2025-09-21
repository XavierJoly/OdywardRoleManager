using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using _0900_OdywardRoleManager.Models;
using _0900_OdywardRoleManager.Utils;

namespace _0900_OdywardRoleManager.Services;

public sealed class ExportService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _auditDirectory;

    public ExportService(string? baseDirectory = null)
    {
        var rootDirectory = baseDirectory ?? AppContext.BaseDirectory;
        _auditDirectory = Path.Combine(rootDirectory, Constants.AuditDirectoryName);
        Directory.CreateDirectory(_auditDirectory);
    }

    public async Task<string> SaveAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        var filePath = GetAuditFilePath(entry.Email);
        List<AuditEntry> entries;

        if (File.Exists(filePath))
        {
            await using var stream = File.OpenRead(filePath);
            entries = await JsonSerializer.DeserializeAsync<List<AuditEntry>>(stream, SerializerOptions, cancellationToken)
                      ?? new List<AuditEntry>();
        }
        else
        {
            entries = new List<AuditEntry>();
        }

        entries.Add(entry);

        await using (var output = File.Create(filePath))
        {
            await JsonSerializer.SerializeAsync(output, entries, SerializerOptions, cancellationToken);
        }

        return filePath;
    }

    public async Task<string> ExportAssignmentReportAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        var reportName = $"rapport-{Sanitize(entry.Email)}-{entry.Timestamp:yyyyMMddHHmmss}.json";
        var filePath = Path.Combine(_auditDirectory, reportName);

        var payload = new
        {
            entry.Email,
            entry.TenantId,
            entry.ActorUpn,
            entry.RolesBefore,
            entry.RolesAfter,
            entry.Timestamp,
        };

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, payload, SerializerOptions, cancellationToken);
        return filePath;
    }

    public async Task<IReadOnlyList<AuditEntry>> ReadAuditEntriesForUserAsync(string email, CancellationToken cancellationToken)
    {
        var filePath = GetAuditFilePath(email);
        if (!File.Exists(filePath))
        {
            return Array.Empty<AuditEntry>();
        }

        await using var stream = File.OpenRead(filePath);
        var entries = await JsonSerializer.DeserializeAsync<List<AuditEntry>>(stream, SerializerOptions, cancellationToken);
        return entries is null ? Array.Empty<AuditEntry>() : entries;
    }

    private string GetAuditFilePath(string email)
    {
        var sanitized = Sanitize(email);
        return Path.Combine(_auditDirectory, $"{sanitized}{Constants.AuditFileExtension}");
    }

    private static string Sanitize(string value)
        => value.Replace("@", "_at_", StringComparison.Ordinal)
                .Replace("/", "_", StringComparison.Ordinal)
                .Replace("\\", "_", StringComparison.Ordinal);
}
