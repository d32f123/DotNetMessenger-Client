using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.RClient;
using DotNetMessenger.RClient.Extensions;
using DotNetMessenger.WPFClient.Router;

namespace DotNetMessenger.WPFClient.ViewModels.Info
{
    [WindowSettings("Manage users", true)]
    public class ManageUsersViewModel : ViewModelBase, IDisposable
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

                _chat = value;
                UsersInfo.Clear();
                _originalInfo.Clear();
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
                        var chatUsers = (await ClientApi.ChatsClient.GetChatAsync(_chat.Id)).Users;
                        foreach (var userId in chatUsers)
                        {
                            var user = await ClientApi.UsersClient.GetUserAsync(userId);
                            UsersInfo.Add(new ChatUserEntryViewModel(_chat.Id, user));
                            _originalInfo.Add(user.Id, (await ClientApi.ChatsClient.GetChatSpecificUserInfoAsync(_chat.Id, user.Id)).CloneJson());
                        }
                    }
                );
            }
        }

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

        // point to remember:
        // if we lose permissions, it is probably wise to abort

        public int Id => _chat?.Id ?? -1;
        public string Title => _chat?.Info?.Title;
        public byte[] Avatar => _chat?.Info?.Avatar;
        public int UsersTotal => _chat?.Users?.Count() ?? -1;

        private readonly Dictionary<int, ChatUserInfo> _originalInfo = new Dictionary<int, ChatUserInfo>();
        public ObservableCollection<ChatUserEntryViewModel> UsersInfo { get; set; } = new ObservableCollection<ChatUserEntryViewModel>();
        public ObservableCollection<User> RemovedUsers { get; set; } = new ObservableCollection<User>();

        private ICommand _removeUserCommand;
        public ICommand RemoveUserCommand
        {
            get
            {
                if (_removeUserCommand == null)
                {
                    _removeUserCommand = new RelayCommand(RemoveUser, o => !IsCreator(o) && !IsSelf(o));
                }
                return _removeUserCommand;
            }
        }

        private ICommand _saveChangesCommand;
        public ICommand SaveChangesCommand
        {
            get
            {
                if (_saveChangesCommand == null)
                {
                    _saveChangesCommand = new RelayCommand(o => SaveChanges());
                }
                return _saveChangesCommand;
            }
        }

        public void RemoveUser(object o)
        {
            if (!(o is ChatUserEntryViewModel vm)) throw new ArgumentException("Not a ViewModel");
            UsersInfo.Remove(vm);
            RemovedUsers.Add(vm.User);
        }

        public static bool IsCreator(object o)
        {
            if (!(o is ChatUserEntryViewModel vm)) return false;
            return vm.IsCreator;
        }

        public static bool IsSelf(object o)
        {
            if (!(o is ChatUserEntryViewModel vm)) return false;
            return vm.User.Id == ClientApi.UserId;
        }

        public async void SaveChanges()
        {
            await ClientApi.ChatsClient.KickUsersAsync(_chat.Id, RemovedUsers.Select(x => x.Id));
            foreach (var vm in UsersInfo)
            {
                if (!Equals(vm.ChatUserInfo, _originalInfo[vm.User.Id]))
                {
                    await ClientApi.ChatsClient.SetChatSpecificUserRoleAsync(_chat.Id, vm.User.Id,
                        vm.ChatUserInfo.Role.RoleType);
                }
            }
            CloseCommand.Execute(null);
        }

        public ManageUsersViewModel()
        {
            Chat = null;
        }

        public ManageUsersViewModel(Chat chat)
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