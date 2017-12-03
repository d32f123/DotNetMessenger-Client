using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using DotNetMessenger.RClient;
using DotNetMessenger.WPFClient.Router;
using DotNetMessenger.WPFClient.ViewModels.Auth;
using LoginWindow = DotNetMessenger.WPFClient.Windows.LoginWindow;
using RegisterWindow = DotNetMessenger.WPFClient.Windows.RegisterWindow;

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
                var welcomeModel = new WelcomeViewModel();
                ViewHostBuilder.GetViewHost().HostView(welcomeModel);

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
