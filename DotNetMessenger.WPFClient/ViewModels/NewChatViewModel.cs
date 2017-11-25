using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DotNetMessenger.Model;
using DotNetMessenger.RClient;
using DotNetMessenger.WPFClient.ViewModels.Entities;

namespace DotNetMessenger.WPFClient.ViewModels
{
    public class NewChatViewModel : ViewModelBase
    {
        #region Properties
        public class UserWithCheckBox
        {
            public bool IsChecked { get; set; }
            public UserViewModel UserViewModel { get; set; }

            public UserWithCheckBox(User user)
            {
                UserViewModel = new UserViewModel(user);
                IsChecked = false;
            }
        }
        public ObservableCollection<UserWithCheckBox> UsersWithChecks { get; set; } = new ObservableCollection<UserWithCheckBox>();
        public IEnumerable<User> Users
        {
            get => UsersWithChecks.Select(x => x.UserViewModel.CurrentUser);
            set
            {
                UsersWithChecks.Clear();
                foreach (var user in value)
                {
                    UsersWithChecks.Add(new UserWithCheckBox(user));
                }
                OnPropertyChanged(nameof(Users));
                OnPropertyChanged(nameof(UsersWithChecks));
                OnPropertyChanged(nameof(SelectedUsers));
            }
        }
        public IEnumerable<User> SelectedUsers => UsersWithChecks.Where(x => x.IsChecked).Select(x => x.UserViewModel.CurrentUser);

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
        #endregion

        #region Commands
        private ICommand _createChatCommand;
        public ICommand CreateChatCommand
        {
            get
            {
                if (_createChatCommand == null)
                {
                    _createChatCommand = new RelayCommand(
                        async x =>
                        {
                            await ClientApi.ChatsClient.CreateNewGroupChat(Title, SelectedUsers.Select(u => u.Id));
                            CloseCommand.Execute(x);
                        },
                        x => !string.IsNullOrEmpty(Title) && SelectedUsers.Any());
                }
                return _createChatCommand;
            }
        }
        #endregion

        #region Constructors
        public NewChatViewModel()
        {}

        public NewChatViewModel(IEnumerable<User> users)
        {
            Users = users;
        }
        #endregion
    }
}