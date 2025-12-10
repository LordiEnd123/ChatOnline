using ChatServer.Dtos;
using ChatServer.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // Регистрация нового пользователя
    [HttpPost("register")]
    public ActionResult<UserDto> Register([FromBody] RegisterRequest request)
    {
        if (UserStore.GetByEmail(request.Email) != null)
        {
            return BadRequest("Пользователь с таким email уже существует.");
        }

        var user = new User
        {
            Email = request.Email,
            Password = request.Password,
            Name = request.Name,
            Status = UserStatus.Online
        };
        UserStore.AddUser(user);
        return Ok(UserDto.FromUser(user));
    }

    /// Логин по email и паролю
    [HttpPost("login")]
    public ActionResult<UserDto> Login([FromBody] LoginRequest request)
    {
        var user = UserStore.GetByEmail(request.Email);
        if (user == null || user.Password != request.Password)
        {
            return Unauthorized("Неверный email или пароль.");
        }
        user.Status = UserStatus.Online;
        UserStore.UpdateUser(user);
        return Ok(UserDto.FromUser(user));
    }

    // Восстановление пароля по email
    [HttpPost("restore")]
    public ActionResult RestorePassword([FromBody] RestorePasswordRequest request)
    {
        var user = UserStore.GetByEmail(request.Email);
        if (user == null)
        {
            return Ok("Если такой email существует, на него отправлена инструкция по восстановлению.");
        }
        return Ok("На вашу почту отправлена ссылка для смены пароля (симуляция).");
    }

    // Список всех пользователей (для теста/поиска контактов)
    [HttpGet("users")]
    public ActionResult<IEnumerable<UserDto>> GetUsers()
    {
        var users = UserStore.GetAll().Select(UserDto.FromUser);
        return Ok(users);
    }
}
