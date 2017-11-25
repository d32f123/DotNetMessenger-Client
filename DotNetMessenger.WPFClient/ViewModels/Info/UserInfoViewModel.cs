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
    public class UserInfoViewModel : ViewModelBase
    {
        private User _user;
        public User User
        {
            get => _user;
            set
            {
                if (Equals(_user, value)) return;
                _user = value;
                OnPropertyChanged(nameof(User));
                OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(Avatar));
                OnPropertyChanged(nameof(FirstName));
                OnPropertyChanged(nameof(LastName));
                OnPropertyChanged(nameof(DateOfBirth));
                OnPropertyChanged(nameof(Gender));
                OnPropertyChanged(nameof(Phone));
                OnPropertyChanged(nameof(Email));
            }
        }

        public string Username => _user?.Username;
        public byte[] Avatar => _user?.UserInfo?.Avatar;
        public string FirstName => _user?.UserInfo?.FirstName;
        public string LastName => _user?.UserInfo?.LastName;
        public DateTime DateOfBirth => _user?.UserInfo?.DateOfBirth ?? DateTime.MinValue;
        public Genders Gender => _user?.UserInfo?.Gender ?? Genders.Unknown;
        public string Phone => _user?.UserInfo?.Phone;
        public string Email => _user?.UserInfo?.Email;


        public UserInfoViewModel() : this(null)
        {
        }


        public UserInfoViewModel(User user)
        {
            User = user;
        }
    }
}