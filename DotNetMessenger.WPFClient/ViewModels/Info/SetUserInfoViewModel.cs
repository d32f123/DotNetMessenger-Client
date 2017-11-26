using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using com.google.i18n.phonenumbers;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.WPFClient.Validation;
using java.lang;

namespace DotNetMessenger.WPFClient.ViewModels.Info
{
    public class SetUserInfoViewModel : ViewModelBase, IDataErrorInfo
    {
        // do not touch ( this value is auto-generated )
        private const string EmailRegexString = @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?";

        private readonly ValidationErrorContainer _errorContainer = new ValidationErrorContainer();

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
                _userInfo = _user?.UserInfo ?? new UserInfo();
            }
        }

        private UserInfo _userInfo;

        public string Username => _user?.Username;
        public byte[] Avatar => _user?.UserInfo?.Avatar;

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
            get => _userInfo?.DateOfBirth ?? DateTime.MinValue;
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

        public SetUserInfoViewModel() : this(new User()) { }
        public SetUserInfoViewModel(User user)
        {
            User = user;
        }

        private void OnErrorsChanged(object sender, DataErrorsChangedEventArgs dataErrorsChangedEventArgs)
        {
            OnPropertyChanged("CurrentValidationError");
        }

        public virtual ValidationCustomError CurrentValidationError
        {
            get
            {
                if (_errorContainer.ErrorCount == 0)
                    return null;

                // Get the error list associated with the last property to be validated.
                using (var p = _errorContainer.Errors[_errorContainer.LastPropertyValidated].GetEnumerator())
                {

                    // Decide which error needs to be returned.
                    ValidationCustomError error = null;
                    while (p.MoveNext())
                    {
                        error = p.Current;
                        if (error == null) continue;
                        if (error.Id == nameof(ExceptionValidationRule))
                            break;
                    }
                    return error;
                }
            }
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

        private void SaveUserInfo()
        {
#if DEBUG
            if (App.IsDesignMode) return;
#endif
            throw new NotImplementedException();
            CloseCommand.Execute(null);
        }

        public bool CanSaveUserInfo => string.IsNullOrEmpty(ValidateFirstName()) && string.IsNullOrEmpty(ValidateLastName())
                   && string.IsNullOrEmpty(ValidatePhone()) && string.IsNullOrEmpty(ValidateEmail());
    }
}