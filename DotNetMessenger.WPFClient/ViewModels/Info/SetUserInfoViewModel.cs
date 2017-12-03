using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using com.google.i18n.phonenumbers;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.RClient;
using DotNetMessenger.WPFClient.Extensions;
using DotNetMessenger.WPFClient.Router;

namespace DotNetMessenger.WPFClient.ViewModels.Info
{
    [WindowSettings("Set user info", true)]
    public class SetUserInfoViewModel : ViewModelBase, IDataErrorInfo
    {
        // do not touch ( this value is auto-generated (it's acltually not) )
        private const string EmailRegexString = @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?";

        private ICommand _saveCommand;

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new RelayCommand(o => SaveUserInfo(), o => CanSaveUserInfo);
                }
                return _saveCommand;
            }
        }

        private ICommand _attachCommand;
        public ICommand AttachCommand
        {
            get
            {
                if (_attachCommand == null)
                {
                    _attachCommand = new RelayCommand(o => AttachFile());
                }
                return _attachCommand;
            }
        }

        private User _user;
        public User User
        {
            get => _user;
            set
            {
                if (ReferenceEquals(_user, value)) return;
                _user = value;
                OnPropertyChanged(nameof(User));
                OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(CurrentAvatar));
                AvatarPath = string.Empty;
                _userInfo = _user?.UserInfo ?? new UserInfo();
                if (_userInfo.DateOfBirth == null) _userInfo.DateOfBirth = DateTime.Today.AddYears(-18);
                OnPropertyChanged(nameof(FirstName));
                OnPropertyChanged(nameof(LastName));
                OnPropertyChanged(nameof(DateOfBirth));
                OnPropertyChanged(nameof(Gender));
                OnPropertyChanged(nameof(Phone));
                OnPropertyChanged(nameof(Email));
            }
        }

        private UserInfo _userInfo;

        public string Username => _user?.Username;
        public byte[] CurrentAvatar => _user?.UserInfo?.Avatar;

        public string FirstName
        {
            get => _userInfo.FirstName;
            set
            {
                if (_userInfo.FirstName == value) return;
                _userInfo.FirstName = value;
                OnPropertyChanged(nameof(FirstName));
            }
        }
        public string LastName
        {
            get => _userInfo.LastName;
            set
            {
                if (_userInfo.LastName == value) return;
                _userInfo.LastName = value;
                OnPropertyChanged(nameof(LastName));
            }
        }
        public DateTime DateOfBirth
        {
            get
            {
                Debug.Assert(_userInfo.DateOfBirth != null, "_userInfo.DateOfBirth != null");
                return (DateTime) _userInfo.DateOfBirth;
            }
            set
            {
                if (Equals(_userInfo.DateOfBirth, value)) return;
                _userInfo.DateOfBirth = value;
                OnPropertyChanged(nameof(DateOfBirth));
            }
        }

        public Genders Gender
        {
            get => _userInfo.Gender ?? Genders.Unknown;
            set
            {
                if (Equals(_userInfo.Gender, value)) return;
                _userInfo.Gender = value;
                OnPropertyChanged(nameof(Gender));
            }
        }
        public string Phone
        {
            get => _userInfo.Phone;
            set
            {
                if (_userInfo.Phone == value) return;
                _userInfo.Phone = value;
                OnPropertyChanged(nameof(Phone));
            }
        }
        public string Email
        {
            get => _userInfo.Email;
            set
            {
                if (_userInfo.Email == value) return;
                _userInfo.Email = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        private string _avatarPath;
        public string AvatarPath
        {
            get => _avatarPath;
            set
            {
                if (_avatarPath == value) return;
                _avatarPath = value;
                OnPropertyChanged(nameof(AvatarPath));
            }
        }

        public SetUserInfoViewModel() : this(new User()) { }
        public SetUserInfoViewModel(User user)
        {
            User = user;
        }

        public string this[string propName]
        {
            get
            {
                string error;
                switch (propName)
                {
                    case nameof(FirstName):
                        error = ValidateFirstName();
                        break;
                    case nameof(LastName):
                        error = ValidateLastName();
                        break;
                    case nameof(Phone):
                        error = ValidatePhone();
                        break;
                    case nameof(Email):
                        error = ValidateEmail();
                        break;
                    case nameof(AvatarPath):
                        error = ValidateAvatar();
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

        private string ValidateFirstName()
        {
            if (!string.IsNullOrEmpty(FirstName) && FirstName.Length > 20)
                 return "First name too long!";
            return null;
        }

        private string ValidateLastName()
        {
            if (!string.IsNullOrEmpty(LastName) && LastName.Length > 20)
                return "Last name too long!";
            return null;
        }

        private string ValidatePhone()
        {
            if (string.IsNullOrEmpty(Phone)) return null;

            var util = PhoneNumberUtil.getInstance();
            Phonenumber.PhoneNumber phone;
            try
            {
                phone = util.parse(Phone, "RU");
            }
            catch (NumberParseException)
            {
                return "Invalid phone number";
            }
           
            return util.isValidNumber(phone) ? null : "Invalid phone number";
        }

        private string ValidateEmail()
        {
            if (string.IsNullOrEmpty(Email)) return null;

            var regex = new Regex(EmailRegexString);
            return regex.IsMatch(Email) ? null : "Email is invalid";
        }

        public string Error => string.Empty;

        private void AttachFile()
        {
            var filename = "";
            if (!FileDialogHelpers.GetImageFromDialog(ref filename)) return;
            var length = new System.IO.FileInfo(filename).Length;
            // if length > 30 MB = 30 MB * 1024 kb/mb * 1024 b/kb
            if (length > 30 * 1024 * 1024)
            {
                MessageBox.Show("This file is way too large! Please something up to 30 mb only", "File too big",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            AvatarPath = filename;
        }

        private string ValidateAvatar()
        {
            if (string.IsNullOrEmpty(AvatarPath))
                return string.Empty;
            try
            {
                Image.FromFile(AvatarPath);
            }
            catch
            {
                return "File is not an image";
            }
            return string.Empty;
        }

        private void SaveUserInfo()
        {
#if DEBUG
            if (App.IsDesignMode) return;
#endif
            _userInfo.Avatar = string.IsNullOrEmpty(AvatarPath)
                ? CurrentAvatar
                : Image.FromFile(AvatarPath).Resize(60, 60, false).ToBytes();
            ClientApi.UsersClient.SetUserInfoAsync(_userInfo);
            CloseCommand.Execute(null);
        }

        public bool CanSaveUserInfo => string.IsNullOrEmpty(ValidateFirstName()) &&
                                       string.IsNullOrEmpty(ValidateLastName()) &&
                                       string.IsNullOrEmpty(ValidatePhone()) &&
                                       string.IsNullOrEmpty(ValidateEmail()) && 
                                       string.IsNullOrEmpty(ValidateAvatar());
    }
}