using NUnit.Framework;

namespace SafeVaultMicrosoftCourse.Tests;

[TestFixture]
public class TestAuthentication
{
    [Test]
    public void PasswordHash_IsNotTheOriginalPassword()
    {
        const string password = "MyStrongPass123!";

        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        Assert.That(hash, Is.Not.EqualTo(password));
        Assert.That(
            BCrypt.Net.BCrypt.Verify(password, hash),
            Is.True);
    }

    [Test]
    public void PasswordVerification_RejectsIncorrectPassword()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(
            "MyStrongPass123!");

        Assert.That(
            BCrypt.Net.BCrypt.Verify("WrongPassword123!", hash),
            Is.False);
    }
}