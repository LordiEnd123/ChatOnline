using ChatServer.Dtos;
using ChatServer.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    /// <summary>
    /// Получить профиль по email
    /// </summary>
    [HttpGet("{email}")]
    public ActionResult<UserDto> GetProfile(string email)
    {
        var user = UserStore.GetByEmail(email);
        if (user == null)
            return NotFound("Пользователь не найден.");

        return Ok(UserDto.FromUser(user));
    }

    /// <summary>
    /// Обновить профиль (имя, аватар, био, статус, уведомления)
    /// </summary>
    [HttpPut("update")]
    public ActionResult<UserDto> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = UserStore.GetByEmail(request.Email);
        if (user == null)
            return NotFound("Пользователь не найден.");

        user.Name = request.Name;
        user.AvatarUrl = request.AvatarUrl;
        user.Bio = request.Bio;
        user.Status = request.Status;
        user.NotificationsEnabled = request.NotificationsEnabled;
        user.SoundEnabled = request.SoundEnabled;
        user.BannerEnabled = request.BannerEnabled;

        UserStore.UpdateUser(user);

        return Ok(UserDto.FromUser(user));
    }

    /// <summary>
    /// Смена email
    /// </summary>
    [HttpPost("change-email")]
    public ActionResult<UserDto> ChangeEmail([FromBody] ChangeEmailRequest request)
    {
        var user = UserStore.GetByEmail(request.OldEmail);
        if (user == null)
            return NotFound("Пользователь не найден.");

        if (!request.NewEmail.Contains("@"))
            return BadRequest("Некорректный email.");

        var other = UserStore.GetByEmail(request.NewEmail);
        if (other != null && other.Id != user.Id)
            return BadRequest("Пользователь с таким email уже существует.");

        user.Email = request.NewEmail;
        UserStore.UpdateUser(user);

        return Ok(UserDto.FromUser(user));
    }

    /// <summary>
    /// Смена пароля
    /// </summary>
    [HttpPost("change-password")]
    public ActionResult ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = UserStore.GetByEmail(request.Email);
        if (user == null)
            return NotFound("Пользователь не найден.");

        if (user.Password != request.OldPassword)
            return BadRequest("Старый пароль неверен.");

        user.Password = request.NewPassword;
        UserStore.UpdateUser(user);

        return Ok("Пароль успешно изменён.");
    }
}
