using System.Windows.Input;

namespace DotNetMessenger.WPFClient.ViewModels
{
    public class ContextAction : ViewModelBase
    {
        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private ICommand _action;

        public ICommand Action
        {
            get => _action;
            set
            {
                if (_action == value) return;
                _action = value;
                OnPropertyChanged(nameof(Action));
            }
        }
    }
}