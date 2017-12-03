using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.RClient;
using DotNetMessenger.WPFClient.Router;
using DotNetMessenger.WPFClient.ViewModels.Entities;

namespace DotNetMessenger.WPFClient.ViewModels.Info
{
    [WindowSettings("Add users", true)]
    public class AddUsersViewModel : ViewModelBase, IDisposable
    {
        private Chat _chat;
        public Chat Chat
        {
            get => _chat;
            set
            {
                if (ReferenceEquals(_chat, value)) return;

                if (_chat != null)
                {
                    ClientApi.ChatsClient.UnsubscribeFromNewChatUserInfo(_chat.Id, ClientApi.UserId, NewUserInfoHandler);
                    ClientApi.ChatsClient.LostChatsEvent -= LostChatsEvent;
                }

                UsersWithChecks.Clear();
                _chat = value;
                OnPropertyChanged(nameof(Chat));
                OnPropertyChanged(nameof(Id));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Avatar));
                OnPropertyChanged(nameof(UsersTotal));

                if (_chat == null) return;

                ClientApi.ChatsClient.SubscribeToNewChatUserInfo(_chat.Id, ClientApi.UserId, NewUserInfoHandler);
                ClientApi.ChatsClient.LostChatsEvent += LostChatsEvent;

                Application.Current.Dispatcher.Invoke(async () =>
                    {
                        var allUsers = await ClientApi.UsersClient.GetAllUsersAsync();
                        var chatUsers = (await ClientApi.ChatsClient.GetChatAsync(_chat.Id)).Users;
                        foreach (var userId in allUsers.Select(x => x.Id).Except(chatUsers))
                        {
                            var user = await ClientApi.UsersClient.GetUserAsync(userId);
                            UsersWithChecks.Add(new UserWithCheckBox(user));
                        }
                    }
                );
            }
        }

        private ICommand _addUsersCommand;
        public ICommand AddUsersCommand
        {
            get
            {
                if (_addUsersCommand == null)
                {
                    _addUsersCommand = new RelayCommand(o => AddUsers(), o => SelectedUsers.Any());
                }
                return _addUsersCommand;
            }
        }

        public async void AddUsers()
        {
            await ClientApi.ChatsClient.AddUsersAsync(_chat.Id, SelectedUsers.Select(x => x.Id));
            CloseCommand.Execute(null);
        }

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
        public IEnumerable<User> SelectedUsers => UsersWithChecks.Where(x => x.IsChecked).Select(x => x.UserViewModel.CurrentUser);

        private void LostChatsEvent(object sender, IEnumerable<int> enumerable)
        {
            if (!enumerable.Contains(_chat?.Id ?? -1)) return;
            // we lost permissions, whoops
            MessageBox.Show("You've just lost permissions to manage users! Whoops", "Whoops!", MessageBoxButton.OK,
                MessageBoxImage.Exclamation);
            CloseCommand.Execute(null);
        }

        private void NewUserInfoHandler(object sender, ChatUserInfo chatUserInfo)
        {
            if (chatUserInfo.Role.RolePermissions.HasFlag(RolePermissions.ManageUsersPerm)) return;
            // we lost permissions, whoops
            MessageBox.Show("You've just lost permissions to manage users! Whoops", "Whoops!", MessageBoxButton.OK,
                MessageBoxImage.Exclamation);
            CloseCommand.Execute(null);
        }

        public int Id => _chat?.Id ?? -1;
        public string Title => _chat?.Info?.Title;
        public byte[] Avatar => _chat?.Info?.Avatar;
        public int UsersTotal => _chat?.Users?.Count() ?? -1;

        public AddUsersViewModel()
        {
            Chat = null;
        }

        public AddUsersViewModel(Chat chat)
        {
            Chat = chat;
        }

        public void Dispose()
        {
            if (_chat != null)
            {
                ClientApi.ChatsClient.UnsubscribeFromNewChatUserInfo(_chat.Id, ClientApi.UserId, NewUserInfoHandler);
                ClientApi.ChatsClient.LostChatsEvent -= LostChatsEvent;
            }
        }
    }
}