using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        private HubConnection _connection;

        // Сообщения текущего открытого диалога
        private readonly ObservableCollection<ChatMessageView> _currentMessages = new();

        // Список контактов
        private readonly ObservableCollection<ContactView> _contacts = new();

        // Email текущего собеседника (если выбран диалог), иначе null = общий чат
        private string? _currentDialogEmail;

        public MainWindow() : this(Session.Name ?? "User")
        {
        }

        public MainWindow(string userName)
        {
            InitializeComponent();

            // Показываем имя в текстбоксе
            UserNameTextBox.Text = userName;

            // Привязываем источники данных
            MessagesListBox.ItemsSource = _currentMessages;
            ContactsListBox.ItemsSource = _contacts;

            InitializeConnection();

            // Загрузка контактов (списка пользователей)
            _ = LoadContactsAsync();
        }

        // ================= ИНИЦИАЛИЗАЦИЯ ПОДКЛЮЧЕНИЯ =================

        private void InitializeConnection()
        {
            // адрес хаба
            var hubUrl = "https://localhost:7090/chat";

            // email текущего пользователя (чтобы сервер понимал, кто мы)
            var email = string.IsNullOrEmpty(Session.Email)
                ? "anonymous@example.com"
                : Session.Email;

            var urlWithUser = $"{hubUrl}?user={Uri.EscapeDataString(email)}";

            _connection = new HubConnectionBuilder()
                .WithUrl(urlWithUser)
                .WithAutomaticReconnect()
                .Build();

            // --- старый общий чат (опционально) ---
            _connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    _currentMessages.Add(new ChatMessageView
                    {
                        FromEmail = user,
                        ToEmail = "(всем)",
                        Text = message,
                        Timestamp = DateTime.Now,
                        Status = "Sent"   // строка
                    });
                });
            });



            // --- личные сообщения и операции с ними ---
            _connection.On<object>("ReceivePrivateMessage", OnReceivePrivateMessage);

            _connection.On<int, string>("MessageEdited", OnMessageEdited);
            _connection.On<int>("MessageDeleted", OnMessageDeleted);

            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            try
            {
                await _connection.StartAsync();
                ConnectionStatusTextBlock.Text = "Подключено";
                ConnectionStatusTextBlock.Foreground = Brushes.Green;

                _currentMessages.Add(new ChatMessageView
                {
                    Text = "Подключено к серверу чата.",
                    Timestamp = DateTime.Now,
                    FromEmail = "system"
                });
            }
            catch (Exception ex)
            {
                ConnectionStatusTextBlock.Text = "Ошибка";
                ConnectionStatusTextBlock.Foreground = Brushes.Red;

                _currentMessages.Add(new ChatMessageView
                {
                    Text = $"Не удалось подключиться: {ex.Message}",
                    Timestamp = DateTime.Now,
                    FromEmail = "system"
                });
            }
        }

        // ================= ЗАГРУЗКА КОНТАКТОВ =================

        private async Task LoadContactsAsync()
        {
            try
            {
                using var client = new HttpClient();
                // эндпоинт, который отдаёт список пользователей
                var response = await client.GetAsync("https://localhost:7090/api/Auth/users");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var users = JsonSerializer.Deserialize<List<UserDto>>(json, options) ?? new();

                _contacts.Clear();

                foreach (var u in users.Where(u => u.Email != Session.Email))
                {
                    _contacts.Add(new ContactView
                    {
                        Email = u.Email,
                        Name = u.Name
                    });
                }
            }
            catch
            {
                // для ДЗ можно ничего не делать
            }
        }

        // ================= ВХОДЯЩИЕ СООБЩЕНИЯ =================

        private void OnReceivePrivateMessage(object raw)
        {
            // пришёл анонимный объект -> превращаем в ChatMessageView
            var json = JsonSerializer.Serialize(raw);
            var msg = JsonSerializer.Deserialize<ChatMessageView>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (msg == null) return;

            Dispatcher.Invoke(() =>
            {
                // если диалог не выбран — показываем всё
                if (_currentDialogEmail == null ||
                    msg.FromEmail == _currentDialogEmail ||
                    msg.ToEmail == _currentDialogEmail)
                {
                    _currentMessages.Add(msg);
                }
            });
        }

        private void OnMessageStatusChanged(int id, string status)
        {
            Dispatcher.Invoke(() =>
            {
                var msg = _currentMessages.FirstOrDefault(m => m.Id == id);
                if (msg != null)
                {
                    msg.Status = status;   // теперь типы совпадают
                    var index = _currentMessages.IndexOf(msg);
                    _currentMessages[index] = msg;
                }
            });
        }

        private void OnMessageEdited(int id, string newText)
        {
            Dispatcher.Invoke(() =>
            {
                var msg = _currentMessages.FirstOrDefault(m => m.Id == id);
                if (msg != null)
                {
                    msg.Text = newText;
                    var index = _currentMessages.IndexOf(msg);
                    _currentMessages[index] = msg;
                }
            });
        }

        private void OnMessageDeleted(int id)
        {
            Dispatcher.Invoke(() =>
            {
                var msg = _currentMessages.FirstOrDefault(m => m.Id == id);
                if (msg != null)
                {
                    _currentMessages.Remove(msg);
                }
            });
        }

        // ================= ОТПРАВКА СООБЩЕНИЙ =================

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var text = MessageTextBox.Text;
            if (string.IsNullOrWhiteSpace(text) || _connection == null)
                return;

            try
            {
                if (!string.IsNullOrEmpty(_currentDialogEmail))
                {
                    // ЛИЧНОЕ сообщение выбранному контакту
                    await _connection.InvokeAsync("SendPrivateMessage",
                        _currentDialogEmail,
                        text,
                        false,   // не файл
                        null);   // нет имени файла
                }
                else
                {
                    // Общий чат (как раньше)
                    var user = string.IsNullOrWhiteSpace(UserNameTextBox.Text)
                        ? "Аноним"
                        : UserNameTextBox.Text;

                    await _connection.InvokeAsync("SendMessage", user, text);
                }

                MessageTextBox.Clear();
            }
            catch (Exception ex)
            {
                _currentMessages.Add(new ChatMessageView
                {
                    Text = $"Ошибка при отправке: {ex.Message}",
                    FromEmail = "system",
                    Timestamp = DateTime.Now
                });
            }
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendButton_Click(sender, e);
            }
        }

        // ================= КОНТАКТЫ / ДИАЛОГИ =================

        private async void ContactsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var contact = ContactsListBox.SelectedItem as ContactView;
            if (contact == null || _connection == null ||
                _connection.State != HubConnectionState.Connected)
                return;

            _currentDialogEmail = contact.Email;

            try
            {
                // Получаем историю переписки от хаба
                var messages = await _connection.InvokeAsync<List<ChatMessageView>>(
                    "GetDialogMessages",
                    _currentDialogEmail);

                _currentMessages.Clear();
                foreach (var m in messages)
                    _currentMessages.Add(m);
            }
            catch (Exception ex)
            {
                _currentMessages.Add(new ChatMessageView
                {
                    FromEmail = "system",
                    Text = $"Ошибка при загрузке диалога: {ex.Message}",
                    Timestamp = DateTime.Now
                });
            }
        }

        private void FindContactButton_Click(object sender, RoutedEventArgs e)
        {
            var query = SearchContactTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(query))
                return;

            var found = _contacts.FirstOrDefault(c =>
                c.Email.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

            if (found != null)
            {
                ContactsListBox.SelectedItem = found;
                ContactsListBox.ScrollIntoView(found);
            }
        }

        // ================= ПРОФИЛЬ =================

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = new ProfileWindow
            {
                Owner = this
            };
            profileWindow.ShowDialog();

            // после закрытия профиля обновляем имя
            UserNameTextBox.Text = Session.Name;
        }

        // ================= ВЫХОД =================

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_connection != null)
                {
                    await _connection.StopAsync();
                    await _connection.DisposeAsync();
                    _connection = null;
                }
            }
            catch
            {
                // для ДЗ можно игнорировать
            }

            // чистим “сессию” и файл с логином
            ClientConfig.Clear();
            Session.Email = "";
            Session.Name = "";

            // открываем окно логина
            var loginWindow = new LoginWindow();
            loginWindow.Show();

            // закрываем чат
            Close();
        }
    }
}
