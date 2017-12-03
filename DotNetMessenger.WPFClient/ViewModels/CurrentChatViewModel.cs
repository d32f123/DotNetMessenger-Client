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
using DotNetMessenger.WPFClient.ViewModels.Info;

namespace DotNetMessenger.WPFClient.ViewModels
{
    public class CurrentChatViewModel : ViewModelBase, IDisposable
    {
        private User _user;
        private Chat _chat;

        private bool _isUser;
        private int _chatId = -1;

        public bool IsUser
        {
            get => _isUser;
            set
            {
                if (_isUser == value) return;
                _isUser = value;
                OnPropertyChanged(nameof(IsUser));
            }
        }

        public ObservableCollection<ContextAction> ContextActions { get; set; }

        public User User
        {
            get => _user;
            set
            {
                if (ReferenceEquals(_user, value)) return;
                if (!_isUser && _chat != null)
                {
                    ClientApi.ChatsClient.UnsubscribeFromNewChatMembers(_chatId, NewChatMembersHandler);
                    ClientApi.ChatsClient.UnsubscribeFromNewChatUserInfo(_chatId, ClientApi.UserId, NewChatUserInfoHandler);
                } else if (_isUser && _user != null)
                {
                    ClientApi.UsersClient.UnsubscribeFromNewUserInfo(_user.Id, NewUserInfoHandler);
                }
                IsUser = true;
                _user = value;
                _chat = null;
                Permissions = RolePermissions.NaN;
                Avatar = _user?.UserInfo?.Avatar;
                Title = _user?.Username;
                if (_user == null) return;
                Application.Current.Dispatcher.Invoke(
                    async () =>
                    {
                        _chatId = (await ClientApi.ChatsClient.GetDialogChatAsync(_user.Id)).Id;
                        ClientApi.UsersClient.SubscribeToNewUserInfo(_user.Id, NewUserInfoHandler);
                    }
                );
            }
        }

        public Chat Chat
        {
            get => _chat;
            set
            {
                if (ReferenceEquals(_chat, value)) return;
                if (!_isUser && _chat != null)
                {
                    ClientApi.ChatsClient.UnsubscribeFromNewChatMembers(_chatId, NewChatMembersHandler);
                    ClientApi.ChatsClient.UnsubscribeFromNewChatUserInfo(_chatId, ClientApi.UserId, NewChatUserInfoHandler);
                }
                else if (_isUser && _user != null)
                {
                    ClientApi.UsersClient.UnsubscribeFromNewUserInfo(_user.Id, NewUserInfoHandler);
                }
                IsUser = false;
                _user = null;
                _chat = value;
                Permissions = RolePermissions.NaN;
                Avatar = _chat?.Info?.Avatar;
                Title = _chat?.Info?.Title ?? (_chat?.Id ?? -1).ToString();
                if (_chat == null) return;
                _chatId = _chat.Id;
                SecondaryTitle = "Members: " + _chat.Users.Count();
                Application.Current.Dispatcher.Invoke(async () =>
                {
                    var info = await ClientApi.ChatsClient.GetChatUserInfoAsync(_chat.Id);
                    Permissions = info.Role.RolePermissions;
                    ClientApi.ChatsClient.SubscribeToNewChatInfo(_chatId, NewChatInfoHandler);
                    ClientApi.ChatsClient.SubscribeToNewChatUserInfo(_chatId, ClientApi.UserId, NewChatUserInfoHandler);
                    ClientApi.ChatsClient.SubscribeToNewChatMembers(_chatId, NewChatMembersHandler);
                });
            }
        }

        private byte[] _avatar;
        public byte[] Avatar
        {
            get => _avatar;
            set
            {
                if (_avatar == value) return;
                _avatar = value;
                OnPropertyChanged(nameof(Avatar));
            }
        }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                if (_title == value) return;
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        private string _secondaryTitle;
        public string SecondaryTitle
        {
            get => _secondaryTitle;
            set
            {
                if (_secondaryTitle == value) return;
                _secondaryTitle = value;
                OnPropertyChanged(nameof(SecondaryTitle));
            }
        }

        private RolePermissions _permissions;
        public RolePermissions Permissions
        {
            get => _permissions;
            set
            {
                if (_permissions == value) return;
                _permissions = value;
                OnPropertyChanged(nameof(Permissions));
            }
        }

        private ICommand _setChatInfoCommand;
        public ICommand SetChatInfoCommand
        {
            get
            {
                if (_setChatInfoCommand == null)
                {
                    _setChatInfoCommand = new RelayCommand(
                        o => ShowSetChatInfoViewModel(),
                        o => !IsUser && Permissions.HasFlag(RolePermissions.ChatInfoPerm)
                    );
                }
                return _setChatInfoCommand;
            }
        }

        private ICommand _manageUsersCommand;
        public  ICommand ManageUsersCommand
        {
            get
            {
                if (_manageUsersCommand == null)
                {
                    _manageUsersCommand = new RelayCommand(
                        o => ShowManageUsersViewModel(),
                        o => !IsUser && Permissions.HasFlag(RolePermissions.ManageUsersPerm)
                    );
                }
                return _manageUsersCommand;
            }
        }

        private ICommand _addUsersCommand;

        public ICommand AddUsersCommand
        {
            get
            {
                if (_addUsersCommand == null)
                {
                    _addUsersCommand = new RelayCommand(
                        o => ShowAddUsersViewModel(),
                        o => !IsUser && Permissions.HasFlag(RolePermissions.ManageUsersPerm)
                    );
                }
                return _addUsersCommand;
            }
        }

        public void ShowAdditionalInfo()
        {
            if (!_isUser)
            {
                ViewHostBuilder.GetViewHost().HostViewModal(new ChatInfoViewModel(_chat));
            }
            else
            {
                ViewHostBuilder.GetViewHost().HostViewModal(new UserInfoViewModel(_user));
            }
        }

        public void ShowSetChatInfoViewModel()
        {
            ViewHostBuilder.GetViewHost().HostViewModal(new SetChatInfoViewModel(_chat));
        }

        public void ShowManageUsersViewModel()
        {
            ViewHostBuilder.GetViewHost().HostViewModal(new ManageUsersViewModel(_chat));
        }

        public void ShowAddUsersViewModel()
        {
            ViewHostBuilder.GetViewHost().HostViewModal(new AddUsersViewModel(_chat));
        }

        private void NewChatMembersHandler(object sender, IEnumerable<int> enumerable)
        {
            SecondaryTitle = "Members: " + enumerable.Count();
        }

        private void NewChatUserInfoHandler(object sender, ChatUserInfo chatUserInfo)
        {
            Permissions = chatUserInfo.Role.RolePermissions;
        }

        private void NewChatInfoHandler(object sender, Chat chat)
        {
            Avatar = chat?.Info?.Avatar;
            Title = chat?.Info?.Title;
        }

        private void NewUserInfoHandler(object sender, User user)
        {
            Avatar = user?.UserInfo?.Avatar;
            Title = user?.Username;
        }

        public CurrentChatViewModel() : this(user: null)
        {
            ContextActions = new ObservableCollection<ContextAction>
            {
                new ContextAction
                {
                    Name = "Info",
                    Action = new RelayCommand(
                        o => ShowAdditionalInfo(), o => _user != null || _chat != null)
                }
            };
        }

        public CurrentChatViewModel(User user)
        {
            User = user;
        }

        public CurrentChatViewModel(Chat chat)
        {
            Chat = chat;
        }

        public void Dispose()
        {
            if (!_isUser && _chat != null)
            {
                ClientApi.ChatsClient.UnsubscribeFromNewChatMembers(_chatId, NewChatMembersHandler);
                ClientApi.ChatsClient.UnsubscribeFromNewChatUserInfo(_chatId, ClientApi.UserId, NewChatUserInfoHandler);
            }
            else if (_isUser && _user != null)
            {
                ClientApi.UsersClient.UnsubscribeFromNewUserInfo(_user.Id, NewUserInfoHandler);
            }
        }
    }
}