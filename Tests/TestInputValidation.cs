
using System.Text.Encodings.Web;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using SafeVaultMicrosoftCourse.Services;

namespace SafeVaultMicrosoftCourse.Tests;

[TestFixture]
public class TestInputValidation
{
    [TestCase("leo_dev", true)]
    [TestCase("leo-2026", true)]
    [TestCase("Leo.2026", true)]
    [TestCase("ab", false)]
    [TestCase("admin' OR '1'='1", false)]
    [TestCase("<script>alert(1)</script>", false)]
    [TestCase("", false)]
    public void TestUsernameValidation(string username, bool expected)
    {
        Assert.That(
            InputValidator.IsValidUsername(username),
            Is.EqualTo(expected));
    }

    [TestCase("leo@example.com", true)]
    [TestCase("leo.dev@example.com", true)]
    [TestCase("not-an-email", false)]
    [TestCase("", false)]
    [TestCase("   ", false)]
    public void TestEmailValidation(string email, bool expected)
    {
        Assert.That(
            InputValidator.IsValidEmail(email),
            Is.EqualTo(expected));
    }

    [Test]
    public void TestForSQLInjection()
    {
        const string connectionString =
            "Data Source=SafeVaultTest;Mode=Memory;Cache=Shared";

        // keep the shared in-memory database alive for this test.
        using var keepAlive = new SqliteConnection(connectionString);
        keepAlive.Open();

        var repository = new UserRepository(connectionString);
        repository.InitializeDatabase();

        repository.CreateUser("alice", "alice@example.com");
        repository.CreateUser("bob", "bob@example.com");

        // This must be treated as one literal username,
        // not as executable SQL.
        const string payload = "' OR '1'='1";

        Assert.That(
            repository.UserExists(payload),
            Is.False);

        // Existing users should still be found normally.
        Assert.That(repository.UserExists("alice"), Is.True);
        Assert.That(repository.UserExists("bob"), Is.True);
    }

    [Test]
    public void TestForXSS()
    {
        const string payload = "<script>alert('xss')</script>";

        // The payload must not be accepted as a username.
        Assert.That(
            InputValidator.IsValidUsername(payload),
            Is.False);

        // HTML encoding converts markup into harmless text
        // when inserted into an appropriate HTML text context.
        var encoded = HtmlEncoder.Default.Encode(payload);

        Assert.That(encoded, Does.Not.Contain("<script>"));
        Assert.That(encoded, Does.Not.Contain("</script>"));
        Assert.That(encoded, Does.Contain("&lt;script&gt;"));
        Assert.That(encoded, Does.Contain("&lt;/script&gt;"));
    }
}
