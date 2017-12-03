using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DotNetMessenger.RClient;
using DotNetMessenger.WPFClient.Router;
using DotNetMessenger.WPFClient.Validation;
using ValidationCustomError = DotNetMessenger.WPFClient.Validation.ValidationCustomError;

namespace DotNetMessenger.WPFClient.ViewModels.Auth
{
    [WindowSettings("Login", true)]
    public class LoginViewModel : ViewModelBase, IDataErrorInfo
    {
        private readonly ValidationErrorContainer _errorContainer = new ValidationErrorContainer();
        public ClientApi.UserCredentials Credentials { get; }
        private bool _rememberLogin;
        private ICommand _loginCommand;
        private ICommand _registerCommand;
        private bool _loggedIn;

        public bool LoggedIn
        {
            get => _loggedIn;
            set
            {
                if (_loggedIn == value) return;
                _loggedIn = value;
                OnPropertyChanged(nameof(LoggedIn));
            }
        }

        public string Username
        {
            get => Credentials.Username;
            set
            {
                if (Credentials.Username == value) return;
                Credentials.Username = value;
                OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(Credentials));
            }
        }

        public string Password
        {
            get => Credentials.Password;
            set
            {
                if (Credentials.Password == value) return;
                Credentials.Password = value;
                OnPropertyChanged(nameof(Password));
                OnPropertyChanged(nameof(Credentials));
            }
        }

        public bool RememberLogin
        {
            get => _rememberLogin;
            set
            {
                if (_rememberLogin == value) return;
                _rememberLogin = value;
                OnPropertyChanged(nameof(RememberLogin));
            }
        }

        public ICommand LoginCommand
        {
            get
            {
                if (_loginCommand == null)
                {
                    _loginCommand = new RelayCommand(e => Login(), CanLogin);
                }
                return _loginCommand;
            }
        }

        public ICommand RegisterCommand
        {
            get
            {
                if (_registerCommand == null)
                {
                    _registerCommand = new RelayCommand(e => Register(), CanLogin);
                }
                return _registerCommand;
            }
        }

        public async void Login()
        {
#if DEBUG
            if (App.IsDesignMode) return;
#endif
            var successful = await ClientApi.LoginAsync(Username, Password);
            LoggedIn = successful;
            if (!successful)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _errorContainer.AddError(new ValidationCustomError(nameof(Username), nameof(ExceptionValidationRule),
                        "Invalid Username or Password"));
                });
                return;
            }
            CloseCommand.Execute(null);
        }

        public async void Register()
        {
#if DEBUG
            if (App.IsDesignMode) return;
#endif
            var successful = await ClientApi.RegisterAsync(Username, Password) != null;
            LoggedIn = successful;
            if (!successful)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _errorContainer.AddError(new ValidationCustomError(nameof(Username), nameof(ExceptionValidationRule),
                        "Invalid Username or Password"));
                });
                return;
            }
            CloseCommand.Execute(null);
        }

        public LoginViewModel()
        {
            Credentials = new ClientApi.UserCredentials();
            _errorContainer.ErrorsChanged += OnErrorsChanged;
        }

        private void OnErrorsChanged(object sender, DataErrorsChangedEventArgs dataErrorsChangedEventArgs)
        {
            OnPropertyChanged("CurrentValidationError");
        }

        public virtual ValidationCustomError CurrentValidationError
        {
            get
            {
                if (_errorContainer.ErrorCount == 0)
                    return null;

                // Get the error list associated with the last property to be validated.
                using (var p = _errorContainer.Errors[_errorContainer.LastPropertyValidated].GetEnumerator())
                {

                    // Decide which error needs to be returned.
                    ValidationCustomError error = null;
                    while (p.MoveNext())
                    {
                        error = p.Current;
                        if (error == null) continue;
                        if (error.Id == nameof(ExceptionValidationRule))
                            break;
                    }
                    return error;
                }
            }
        }

        string IDataErrorInfo.this[string propertyName]
        {
            get
            {
                string error;
                switch (propertyName)
                {
                    case nameof(Username):
                        error = ValidateUsername();
                        break;
                    case nameof(Password):
                        error = ValidatePassword();
                        break;
                    default:
                        error = null;
                        break;
                }
                // Dirty the commands registered with CommandManager, 
                // such as our Save command, so that they are queried 
                // to see if they can execute now. 
                CommandManager.InvalidateRequerySuggested();
                return error;
            }
        }
        private string ValidateUsername()
        {
            if (string.IsNullOrEmpty(Credentials.Username) || Credentials.Username.Length < 4)
                return "Username is too short";
            if (Credentials.Username.Length >= 16)
                return "Username is too long";
            return string.Empty;
        }

        private string ValidatePassword()
        {
            if (string.IsNullOrEmpty(Credentials.Password))
                return "Password is too short";
            if (Credentials.Password.Length >= 20)
                return "Password is too long";
            return string.Empty;
        }

        public string Error => string.Empty;

        public bool CanLogin(object e)
        {
            return string.IsNullOrEmpty(ValidateUsername()) && string.IsNullOrEmpty(ValidatePassword());
        }
    }
}