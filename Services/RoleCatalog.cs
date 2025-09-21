using System;
using System.Collections.Generic;
using System.Linq;
using _0900_OdywardRoleManager.Models;

namespace _0900_OdywardRoleManager.Services;

public sealed class RoleCatalog
{
    private readonly IReadOnlyDictionary<string, RoleDefinition> _roles;

    public RoleCatalog()
    {
        var roles = new List<RoleDefinition>
        {
            new("Application Administrator", "62e90394-69f5-4237-9190-012177145e10", "Gestion des applications d'entreprise."),
            new("Groups Administrator", "fdd7a751-b60b-444a-984c-02652fe8fa1c", "Gestion des groupes et licences associées."),
            new("User Administrator", "fe930be7-5e62-47db-91af-98c3a49a38b1", "Gestion des utilisateurs et des mots de passe."),
        };

        _roles = roles.ToDictionary(static r => r.DisplayName, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<RoleModel> CreateRoleModels()
        => _roles.Values.Select(definition => new RoleModel
        {
            DisplayName = definition.DisplayName,
            TemplateId = definition.RoleTemplateId,
            Description = definition.Description,
        });

    public bool TryGetDefinition(string displayName, out RoleDefinition definition)
        => _roles.TryGetValue(displayName, out definition!);

    public sealed record RoleDefinition(string DisplayName, string RoleTemplateId, string Description);
}
