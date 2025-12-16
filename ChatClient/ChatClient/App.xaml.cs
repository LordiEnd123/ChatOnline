using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Windows;

namespace ChatClient
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Инициализируем трей один раз при старте
            NotificationService.InitTray();

            // Открываем логин (если у тебя StartupUri — можно убрать этот кусок)
            var w = new LoginWindow();
            w.Show();
        }
    }
}
