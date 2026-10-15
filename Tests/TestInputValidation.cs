
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using SafeVaultMicrosoftCourse.Services;

namespace SafeVaultMicrosoftCourse.Tests;

[TestFixture]
public class TestInputValidation
{
    private SqliteConnection _keepAliveConnection = null!;
    private UserRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var connectionString =
            $"Data Source=SafeVaultTest_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

        // Keep the in-memory database alive for the duration of each test.
        _keepAliveConnection = new SqliteConnection(connectionString);
        _keepAliveConnection.Open();

        _repository = new UserRepository(connectionString);
        _repository.InitializeDatabase();

        // Seed a legitimate user for database-query tests.
        _repository.CreateUser(
            "alice",
            "alice@example.com",
            BCrypt.Net.BCrypt.HashPassword("ExamplePassword123!"));
    }

    [TearDown]
    public void TearDown()
    {
        _keepAliveConnection.Dispose();
    }

    [TestCase("' OR 1=1 --")]
    [TestCase("admin' OR '1'='1")]
    [TestCase("<script>alert(1)</script>")]
    public void UsernameValidation_RejectsMaliciousInput(string input)
    {
        Assert.That(
            InputValidator.IsValidUsername(input),
            Is.False);
    }

    [Test]
    public void EmailValidation_RejectsScriptPayload()
    {
        Assert.That(
            InputValidator.IsValidEmail(
                "alice@example.com<script>alert(1)</script>"),
            Is.False);
    }

    [Test]
    public void UserExists_UsesLiteralUsernameInsteadOfSqlInjection()
    {
        Assert.That(_repository.UserExists("alice"), Is.True);

        // This input must be treated as a username, not executable SQL.
        Assert.That(
            _repository.UserExists("' OR 1=1 --"),
            Is.False);
    }

    [Test]
    public void UserExists_DoesNotReturnTrueForArbitrarySqlPayload()
    {
        Assert.That(
            _repository.UserExists("' OR '1'='1' --"),
            Is.False);
    }
}
