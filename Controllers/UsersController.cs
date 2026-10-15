using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using SafeVaultMicrosoftCourse.Models;
using SafeVaultMicrosoftCourse.Services;

namespace SafeVaultMicrosoftCourse.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserRepository _repository;

    public UsersController(UserRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var username = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        var user = _repository.GetUserByUsername(username);

        if (user is null) return NotFound();

        return Ok(new
        {
            user.UserID,
            user.Username,
            user.Email,
            user.Role
        });
    }
}