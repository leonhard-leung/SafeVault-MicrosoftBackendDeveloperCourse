using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using SafeVaultMicrosoftCourse.Models;
using SafeVaultMicrosoftCourse.Services;

namespace SafeVaultMicrosoftCourse.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserRepository _repository;

    public UsersController(UserRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] User submission)
    {
        if (submission is null)
        {
            return BadRequest(new { error = "User data is required." });
        }

        if (!InputValidator.IsValidUsername(submission.Username))
        {
            return BadRequest(new { error = "Invalid username." });
        }

        if (!InputValidator.IsValidEmail(submission.Email))
        {
            return BadRequest(new { error = "Invalid email address." });
        }

        var username = submission.Username!;
        var email = submission.Email!.Trim();

        if (_repository.UserExists(username))
        {
            return Conflict(new { error = "Username already exists." });
        }

        try
        {
            var userId = _repository.CreateUser(username, email);

            return Created(
                $"/api/users/{userId}",
                new
                {
                    userId,
                    username,
                    email
                });
        }
        catch (SqliteException ex)
            when (ex.SqliteExtendedErrorCode == 2067)
        {
            // SQLite extended error 2067:
            // UNIQUE constraint violation.
            return Conflict(
                new { error = "Username already exists." });
        }
    }
}