using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        private HubConnection _connection;

        public MainWindow() : this("User")
        {
        }

        public MainWindow(string userName)
        {
            InitializeComponent();
            UserNameTextBox.Text = userName;
            InitializeConnection();
        }

        private void InitializeConnection()
        {
            // ВАЖНО: подставь сюда свой порт из Swagger
            // Например, если Swagger: https://localhost:7090/swagger/index.html
            // то адрес хаба будет:   https://localhost:7090/chat
            var hubUrl = "https://localhost:7090/chat";

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            // Обработка входящих сообщений
            _connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    MessagesListBox.Items.Add($"{user}: {message}");
                });
            });

            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            try
            {
                await _connection.StartAsync();
                ConnectionStatusTextBlock.Text = "Подключено";
                ConnectionStatusTextBlock.Foreground =
                    System.Windows.Media.Brushes.Green;

                MessagesListBox.Items.Add("Подключено к серверу чата.");
            }
            catch (Exception ex)
            {
                ConnectionStatusTextBlock.Text = "Ошибка";
                ConnectionStatusTextBlock.Foreground =
                    System.Windows.Media.Brushes.Red;

                MessagesListBox.Items.Add($"Не удалось подключиться: {ex.Message}");
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MessageTextBox.Text))
                return;

            var user = string.IsNullOrWhiteSpace(UserNameTextBox.Text)
                ? "Аноним"
                : UserNameTextBox.Text;

            try
            {
                await _connection.InvokeAsync("SendMessage", user, MessageTextBox.Text);
                MessageTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessagesListBox.Items.Add($"Ошибка при отправке: {ex.Message}");
            }
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendButton_Click(sender, e);
            }
        }
    }
}
