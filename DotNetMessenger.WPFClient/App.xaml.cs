using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using DotNetMessenger.RClient;
using DotNetMessenger.WPFClient.ViewModels.Auth;
using LoginWindow = DotNetMessenger.WPFClient.Windows.LoginWindow;
using RegisterWindow = DotNetMessenger.WPFClient.Windows.RegisterWindow;
using WelcomeWindow = DotNetMessenger.WPFClient.Windows.WelcomeWindow;

namespace DotNetMessenger.WPFClient
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App
    {
        public static bool IsDesignMode =>
            System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject());


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var oldShutdownMode = ShutdownMode;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            while (true)
            {
                var welcomeWindow = new WelcomeWindow();
                var welcomeModel = new WelcomeViewModel();
                welcomeWindow.DataContext = welcomeModel;
                welcomeModel.CloseRequested += (sender, args) => Current.Dispatcher.Invoke(() => welcomeWindow.Close());
                welcomeWindow.ShowDialog();


                if (welcomeModel.IsRegistered == null)
                {
                    Current.Shutdown();
                    return;
                }
                if (!(bool)welcomeModel.IsRegistered)
                {
                    var registerWindow = new RegisterWindow();
                    var registerViewModel = new LoginViewModel();
                    registerWindow.DataContext = registerViewModel;
                    registerViewModel.CloseRequested += (sender, args) => registerWindow.Close();
                    registerWindow.ShowDialog();
                    if (!registerViewModel.LoggedIn)
                        continue;
                }
                var loginWindow = new LoginWindow();
                var loginViewModel = new LoginViewModel();
                loginWindow.DataContext = loginViewModel;
                loginViewModel.CloseRequested += (sender, args) => loginWindow.Close();
                loginWindow.ShowDialog();
                if (!loginViewModel.LoggedIn)
                    continue;
                break;
            }

            ShutdownMode = oldShutdownMode;
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            ClientApi.LogOut().Wait();
        }

        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Trace.Write(e.ToString());
        }
    }
}
