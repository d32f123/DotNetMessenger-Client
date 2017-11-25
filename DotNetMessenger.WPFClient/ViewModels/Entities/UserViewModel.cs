using System;
using System.Collections.ObjectModel;
using DotNetMessenger.Model;
using DotNetMessenger.WPFClient.Router;
using DotNetMessenger.WPFClient.ViewModels.Info;

namespace DotNetMessenger.WPFClient.ViewModels.Entities
{
    public class UserViewModel : EntityViewModel
    {
        private User _currentUser;
        public User CurrentUser
        {
            get => _currentUser;
            set
            {
                if (value == null && _currentUser == null) return;
                if (value != null && _currentUser != null && value.Equals(_currentUser)) return;
                _currentUser = value;
                OnPropertyChanged(nameof(CurrentUser));
                OnPropertyChanged(nameof(MainString));
                OnPropertyChanged(nameof(SecondaryString));
                OnPropertyChanged(nameof(Image));
            }
        }

        public UserViewModel() : this(new User())
        {
        }

        public UserViewModel(User user)
        {
            CurrentUser = user;
            ContextActions = new ObservableCollection<ContextAction>
            {
                new ContextAction {Name = "Info", Action = new RelayCommand(ShowInfoViewModel)}
            };
        }

        private void ShowInfoViewModel(object o)
        {
            ViewHostBuilder.GetViewHost().HostView(new UserInfoViewModel(_currentUser));
        }

        public sealed override ObservableCollection<ContextAction> ContextActions { get; set; }

        public override string MainString => _currentUser?.Username ?? string.Empty;

        public override string SecondaryString
        {
            get
            {
                if (_currentUser?.UserInfo == null)
                    return string.Empty;
                if (_currentUser.UserInfo.FirstName == null)
                    return _currentUser?.UserInfo?.LastName ?? string.Empty;
                if (_currentUser.UserInfo.LastName == null)
                    return _currentUser.UserInfo.FirstName;
                return _currentUser.UserInfo.LastName + _currentUser.UserInfo.FirstName;
            }
        }
        public override byte[] Image => _currentUser?.UserInfo?.Avatar;
        public override DateTime Date => DateTime.MinValue;
    }
}