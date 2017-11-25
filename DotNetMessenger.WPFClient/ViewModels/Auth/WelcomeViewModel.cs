using System.Windows.Input;

namespace DotNetMessenger.WPFClient.ViewModels.Auth
{
    public class WelcomeViewModel : ViewModelBase
    {
        private bool? _isRegistered;

        private ICommand _loginCommand;
        private ICommand _registerCommand;

        public bool? IsRegistered
        {
            get => _isRegistered;
            set
            {
                if (_isRegistered == value) return;
                _isRegistered = value;
                OnPropertyChanged(nameof(IsRegistered));
            }
        }

        public ICommand LoginCommand
        {
            get
            {
                if (_loginCommand == null)
                {
                    _loginCommand = new RelayCommand(x =>
                    {
                        IsRegistered = true;
                        CloseCommand.Execute(null);
                    });
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
                    _registerCommand = new RelayCommand(x =>
                    {
                        IsRegistered = false;
                        CloseCommand.Execute(null);
                    });
                }
                return _registerCommand;
            }
        }
        
    }
}