using ChatServer.Dtos;
using ChatServer.Models;
using ChatServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Net.Mail;


namespace ChatServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly EmailService _email;

    // если сервер по IP — лучше держать в конфиге, но пока так:
    private const string ServerBaseUrl = "http://192.168.1.105:5099";

    private readonly IHubContext<ChatHub> _hub;

    public AuthController(EmailService email, IHubContext<ChatHub> hub)
    {
        _email = email;
        _hub = hub;
    }


    [HttpPost("register")]
    public ActionResult Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim();

        if (!MailAddress.TryCreate(email, out _))
            return BadRequest("Некорректный email.");

        if (UserStore.GetByEmail(email) != null)
            return BadRequest("Пользователь с таким email уже существует.");

        var token = Guid.NewGuid().ToString("N");

        var user = new User
        {
            Email = email,
            Password = request.Password,
            Name = request.Name,
            Status = UserStatus.Offline,
            EmailConfirmed = false,
            EmailConfirmToken = token
        };

        UserStore.AddUser(user);

        _email.SendConfirmEmail(email, token, ServerBaseUrl);

        return Ok("Регистрация создана. Проверь почту и подтвердите email.");
    }

    [HttpPost("login")]
    public ActionResult<UserDto> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim();

        var user = UserStore.GetByEmail(email);
        if (user == null || user.Password != request.Password)
            return Unauthorized("Неверный email или пароль.");

        if (!user.EmailConfirmed)
            return Unauthorized("Подтвердите email перед входом.");

        user.Status = UserStatus.Online;
        UserStore.UpdateUser(user);

        return Ok(UserDto.FromUser(user));
    }

    [HttpPost("restore")]
    public ActionResult RestorePassword([FromBody] RestorePasswordRequest request)
    {
        var email = request.Email.Trim();
        var user = UserStore.GetByEmail(email);

        if (user != null)
        {
            try
            {
                _email.SendPasswordEmail(user.Email, user.Password);
            }
            catch
            {
                // намеренно молчим
            }
        }

        return Ok("Если такой email существует, пароль отправлен на почту.");
    }

    [HttpGet("users")]
    public ActionResult<IEnumerable<UserDto>> GetUsers()
    {
        var users = UserStore.GetAll().Select(UserDto.FromUser);
        return Ok(users);
    }

    [HttpGet("confirm-email")]
    public ActionResult ConfirmEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest("Token пустой.");

        var user = UserStore.GetAll().FirstOrDefault(u => u.EmailConfirmToken == token);
        if (user == null)
            return NotFound("Токен не найден или устарел.");

        user.EmailConfirmed = true;
        user.EmailConfirmToken = null;
        user.Status = UserStatus.Offline;

        UserStore.UpdateUser(user);
        // уведомляем всех клиентов: появился новый подтвержденный пользователь
        _hub.Clients.All.SendAsync("UserRegistered", UserDto.FromUser(user));

        return Ok("Email подтверждён. Теперь можно войти в приложение.");
    }
}
