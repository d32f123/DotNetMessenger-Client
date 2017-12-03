using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.RClient;

namespace DotNetMessenger.WPFClient.ViewModels.Info
{
    public class ChatUserEntryViewModel : ViewModelBase
    {
        private User _user;
        public User User
        {
            get => _user;
            set
            {
                if (ReferenceEquals(_user, value)) return;
                IsCreator = false;
                _user = value;
                _user.ChatUserInfos = null;
                OnPropertyChanged(nameof(User));
                OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(Avatar));
                OnPropertyChanged(nameof(FirstName));
                OnPropertyChanged(nameof(LastName));
                OnPropertyChanged(nameof(DateOfBirth));
                OnPropertyChanged(nameof(UserRole));
                OnPropertyChanged(nameof(Gender));
                OnPropertyChanged(nameof(Phone));
                OnPropertyChanged(nameof(Email));
                new Thread(FetchInfo).Start();
            }
        }

        private int _chatId;
        public int ChatId
        {
            get => _chatId;
            set
            {
                if (_chatId == value) return;
                _chatId = value;
                IsCreator = false;
                if (_user != null)
                    _user.ChatUserInfos = null;
                OnPropertyChanged(nameof(ChatId));
                OnPropertyChanged(nameof(ChatUserInfo));
                new Thread(FetchInfo).Start();
            }
        }

        private bool _isCreator;
        public bool IsCreator
        {
            get => _isCreator;
            set
            {
                if (_isCreator == value) return;
                _isCreator = value;
                OnPropertyChanged(nameof(IsCreator));
                OnPropertyChanged(nameof(IsNotSelfOrCreator));
            }
        }

        public bool IsNotSelfOrCreator => !IsCreator && _user?.Id != ClientApi.UserId;

        public string Username => _user?.Username;
        public byte[] Avatar => _user?.UserInfo?.Avatar;
        public string FirstName => _user?.UserInfo?.FirstName;
        public string LastName => _user?.UserInfo?.LastName;
        public DateTime DateOfBirth => _user?.UserInfo?.DateOfBirth ?? DateTime.MinValue;
        public Genders Gender => _user?.UserInfo?.Gender ?? Genders.Unknown;
        public string Phone => _user?.UserInfo?.Phone;
        public string Email => _user?.UserInfo?.Email;
        
        public ChatUserInfo ChatUserInfo => _user?.ChatUserInfos?[_chatId];
        public string UserNickname => ChatUserInfo?.Nickname;
        public string RoleName => ChatUserInfo?.Role.RoleName;

        public UserRoles UserRole
        {
            get => ChatUserInfo?.Role?.RoleType ?? UserRoles.Listener;
            set
            {
                if (ChatUserInfo.Role.RoleType == value) return;
                ChatUserInfo.Role.RoleType = value;
                OnPropertyChanged(nameof(UserRole));
            }
        }
        public bool ReadPermission => (ChatUserInfo?.Role.RolePermissions & RolePermissions.ReadPerm) != 0;
        public bool WritePermission => (ChatUserInfo?.Role.RolePermissions & RolePermissions.WritePerm) != 0;
        public bool AttachPermission => (ChatUserInfo?.Role.RolePermissions & RolePermissions.AttachPerm) != 0;
        public bool ChatInfoPermission => (ChatUserInfo?.Role.RolePermissions & RolePermissions.ChatInfoPerm) != 0;
        public bool UserManagementPermission => (ChatUserInfo?.Role.RolePermissions & RolePermissions.ManageUsersPerm) != 0;


        public ChatUserEntryViewModel() : this(-1, null)
        {
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(ChatUserInfo))
            {
                OnPropertyChanged(nameof(UserNickname));
                OnPropertyChanged(nameof(UserRole));
                OnPropertyChanged(nameof(RoleName));
                OnPropertyChanged(nameof(ReadPermission));
                OnPropertyChanged(nameof(WritePermission));
                OnPropertyChanged(nameof(AttachPermission));
                OnPropertyChanged(nameof(ChatInfoPermission));
                OnPropertyChanged(nameof(UserManagementPermission));
                OnPropertyChanged(nameof(IsNotSelfOrCreator));
            }
        }

        public ChatUserEntryViewModel(int chatId, User user)
        {
            _chatId = chatId;
            User = user;
            PropertyChanged += OnPropertyChanged;
        }

        private void FetchInfo()
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                if (_user == null) return;
                var chat = await ClientApi.ChatsClient.GetChatAsync(_chatId);

                if (chat?.CreatorId == _user.Id)
                    IsCreator = true;
                var info = await ClientApi.ChatsClient.GetChatSpecificUserInfoAsync(_chatId, _user.Id);
                _user.ChatUserInfos = new Dictionary<int, ChatUserInfo>
                {
                    {_chatId, info}
                };
                OnPropertyChanged(nameof(ChatUserInfo));
            });
        }
    }
}