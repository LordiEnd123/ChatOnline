using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace ChatClient
{
    public partial class LoginWindow : Window
    {
        private const string BaseUrl = "https://localhost:7090";

        private readonly HttpClient _httpClient = new HttpClient(
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            });

        public LoginWindow()
        {
            InitializeComponent();
        }

        private class LoginRequest
        {
            public string Email { get; set; } = null!;
            public string Password { get; set; } = null!;
        }

        private class UserDto
        {
            public Guid Id { get; set; }
            public string Email { get; set; } = null!;
            public string Name { get; set; } = null!;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "";

            var email = EmailTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                StatusTextBlock.Text = "Введите email и пароль.";
                return;
            }

            try
            {
                var user = await LoginAsync(email, password);
                if (user == null)
                {
                    StatusTextBlock.Text = "Неверный email или пароль.";
                    return;
                }

                OpenChat(user.Name);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Ошибка: " + ex.Message;
            }
        }

        private async Task<UserDto?> LoginAsync(string email, string password)
        {
            var req = new LoginRequest { Email = email, Password = password };
            var json = JsonSerializer.Serialize(req);

            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/api/Auth/login",
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
                return null;

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserDto>(jsonResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                StatusTextBlock.Text = "Введите email для восстановления.";
                return;
            }

            try
            {
                await _httpClient.PostAsync(
                    $"{BaseUrl}/api/Auth/restore",
                    new StringContent($"{{\"email\":\"{email}\"}}", Encoding.UTF8, "application/json"));

                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                StatusTextBlock.Text = "Инструкция отправлена (симуляция).";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                StatusTextBlock.Text = "Ошибка: " + ex.Message;
            }
        }

        private void OpenChat(string name)
        {
            var chat = new MainWindow(name);
            chat.Show();
            this.Close();
        }
    }
}
