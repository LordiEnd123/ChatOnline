using ChatServer.Dtos;
using ChatServer.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
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
            Password = request.Password, // для ДЗ ок
            Name = request.Name,
            Status = UserStatus.Online
        };

        UserStore.AddUser(user);

        return Ok(UserDto.FromUser(user));
    }

    /// <summary>
    /// Логин по email и паролю
    /// </summary>
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

    /// <summary>
    /// "Восстановление" пароля по email (имитация)
    /// </summary>
    [HttpPost("restore")]
    public ActionResult RestorePassword([FromBody] RestorePasswordRequest request)
    {
        var user = UserStore.GetByEmail(request.Email);
        if (user == null)
        {
            // можно вернуть 200, чтобы не палить, что такого email нет
            return Ok("Если такой email существует, на него отправлена инструкция по восстановлению.");
        }

        // Здесь в реальности отправили бы email.
        // Для ДЗ просто считаем, что письмо ушло.
        return Ok("На вашу почту отправлена ссылка для смены пароля (симуляция).");
    }

    /// <summary>
    /// Список всех пользователей (для теста/поиска контактов)
    /// </summary>
    [HttpGet("users")]
    public ActionResult<IEnumerable<UserDto>> GetUsers()
    {
        var users = UserStore.GetAll().Select(UserDto.FromUser);
        return Ok(users);
    }
}
