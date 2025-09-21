using System;
using System.Text.RegularExpressions;

namespace _0900_OdywardRoleManager.Utils;

public static partial class Validation
{
    public static bool IsValidEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            return EmailRegex().IsMatch(value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
