using System.Net.Mail;
using System.Text.RegularExpressions;

namespace SafeVaultMicrosoftCourse.Services;

public static class InputValidator
{
    private static readonly Regex UsernamePattern = 
        new(@"^[A-Za-z0-9_.-]{3,30}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // checks if the username is valid according to the pattern to avoid malicious input
    public static bool IsValidUsername(string? username)
    {
        return !string.IsNullOrEmpty(username) && UsernamePattern.IsMatch(username);
    }
    
    // checks if the email is valid using the built-in MailAddress class to avoid malicious input
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        email = email.Trim();

        if (email.Length > 254) return false;

        try
        {
            var address = new MailAddress(email);

            return string.Equals(
                address.Address,
                email,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}