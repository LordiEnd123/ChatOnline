using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace ChatClient
{
    public partial class ProfileWindow : Window
    {
        private const string BaseUrl = "https://localhost:7090";

        private readonly HttpClient _httpClient = new HttpClient(
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            });

        public ProfileWindow()
        {
            InitializeComponent();
            LoadProfile();
        }

        private class UserDto
        {
            public Guid Id { get; set; }
            public string Email { get; set; } = null!;
            public string Name { get; set; } = null!;
            public string? AvatarUrl { get; set; }
            public string? Bio { get; set; }
            public int Status { get; set; }
            public bool NotificationsEnabled { get; set; }
            public bool SoundEnabled { get; set; }
            public bool BannerEnabled { get; set; }
        }

        private class UpdateProfileRequest
        {
            public string Email { get; set; } = null!;
            public string Name { get; set; } = null!;
            public string? AvatarUrl { get; set; }
            public string? Bio { get; set; }
            public int Status { get; set; }
            public bool NotificationsEnabled { get; set; }
            public bool SoundEnabled { get; set; }
            public bool BannerEnabled { get; set; }
        }

        private class ChangeEmailRequest
        {
            public string OldEmail { get; set; } = null!;
            public string NewEmail { get; set; } = null!;
        }

        private class ChangePasswordRequest
        {
            public string Email { get; set; } = null!;
            public string OldPassword { get; set; } = null!;
            public string NewPassword { get; set; } = null!;
        }

        private async void LoadProfile()
        {
            try
            {
                StatusTextBlock.Text = "";
                var response = await _httpClient.GetAsync($"{BaseUrl}/api/Profile/{Session.Email}");
                if (!response.IsSuccessStatusCode)
                {
                    StatusTextBlock.Text = "Не удалось загрузить профиль.";
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<UserDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (user == null)
                {
                    StatusTextBlock.Text = "Ошибка чтения профиля.";
                    return;
                }

                EmailTextBlock.Text = user.Email;
                NameTextBox.Text = user.Name;
                AvatarTextBox.Text = user.AvatarUrl;
                BioTextBox.Text = user.Bio;

                // Статус
                StatusComboBox.SelectedIndex = user.Status;

                NotificationsEnabledCheckBox.IsChecked = user.NotificationsEnabled;
                SoundEnabledCheckBox.IsChecked = user.SoundEnabled;
                BannerEnabledCheckBox.IsChecked = user.BannerEnabled;

                // Аватар
                if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
                {
                    try
                    {
                        AvatarImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(user.AvatarUrl));
                    }
                    catch
                    {
                        AvatarImage.Source = null;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Ошибка: " + ex.Message;
            }
        }

        private async void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var req = new UpdateProfileRequest
                {
                    Email = Session.Email,
                    Name = NameTextBox.Text.Trim(),
                    AvatarUrl = string.IsNullOrWhiteSpace(AvatarTextBox.Text) ? null : AvatarTextBox.Text.Trim(),
                    Bio = string.IsNullOrWhiteSpace(BioTextBox.Text) ? null : BioTextBox.Text.Trim(),
                    Status = StatusComboBox.SelectedIndex < 0 ? 0 : StatusComboBox.SelectedIndex,
                    NotificationsEnabled = NotificationsEnabledCheckBox.IsChecked == true,
                    SoundEnabled = SoundEnabledCheckBox.IsChecked == true,
                    BannerEnabled = BannerEnabledCheckBox.IsChecked == true
                };

                var json = JsonSerializer.Serialize(req);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{BaseUrl}/api/Profile/update", content);

                if (!response.IsSuccessStatusCode)
                {
                    StatusTextBlock.Foreground = Brushes.Red;
                    StatusTextBlock.Text = "Не удалось сохранить профиль.";
                    return;
                }

                StatusTextBlock.Foreground = Brushes.Green;
                StatusTextBlock.Text = "Профиль сохранён.";
                Session.Name = req.Name;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Foreground = Brushes.Red;
                StatusTextBlock.Text = "Ошибка: " + ex.Message;
            }
        }

        private async void ChangeEmailButton_Click(object sender, RoutedEventArgs e)
        {
            var newEmail = NewEmailTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(newEmail))
            {
                StatusTextBlock.Text = "Введите новый email.";
                return;
            }

            try
            {
                var req = new ChangeEmailRequest
                {
                    OldEmail = Session.Email,
                    NewEmail = newEmail
                };

                var json = JsonSerializer.Serialize(req);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/api/Profile/change-email", content);

                if (!response.IsSuccessStatusCode)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    StatusTextBlock.Foreground = Brushes.Red;
                    StatusTextBlock.Text = "Не удалось изменить email: " + msg;
                    return;
                }

                Session.Email = newEmail;
                EmailTextBlock.Text = newEmail;
                StatusTextBlock.Foreground = Brushes.Green;
                StatusTextBlock.Text = "Email изменён.";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Foreground = Brushes.Red;
                StatusTextBlock.Text = "Ошибка: " + ex.Message;
            }
        }

        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var oldPass = OldPasswordBox.Password;
            var newPass = NewPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(oldPass) || string.IsNullOrWhiteSpace(newPass))
            {
                StatusTextBlock.Text = "Введите старый и новый пароль.";
                return;
            }

            try
            {
                var req = new ChangePasswordRequest
                {
                    Email = Session.Email,
                    OldPassword = oldPass,
                    NewPassword = newPass
                };

                var json = JsonSerializer.Serialize(req);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/api/Profile/change-password", content);

                if (!response.IsSuccessStatusCode)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    StatusTextBlock.Foreground = Brushes.Red;
                    StatusTextBlock.Text = "Не удалось изменить пароль: " + msg;
                    return;
                }

                StatusTextBlock.Foreground = Brushes.Green;
                StatusTextBlock.Text = "Пароль изменён.";
                OldPasswordBox.Password = "";
                NewPasswordBox.Password = "";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Foreground = Brushes.Red;
                StatusTextBlock.Text = "Ошибка: " + ex.Message;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
