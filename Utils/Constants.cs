namespace _0900_OdywardRoleManager.Utils;

public static class Constants
{
    public const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";

    public static readonly string[] RequiredScopes =
    {
        "Directory.ReadWrite.All",
        "RoleManagement.ReadWrite.Directory",
        "User.Read.All"
    };

    public const string AuditDirectoryName = "Logs";

    public const string AuditFileExtension = ".json";
}
