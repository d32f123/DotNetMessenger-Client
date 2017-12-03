using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using DotNetMessenger.Model;
using DotNetMessenger.RClient;
using DotNetMessenger.WPFClient.Extensions;
using DotNetMessenger.WPFClient.Router;

namespace DotNetMessenger.WPFClient.ViewModels.Info
{
    [WindowSettings("Set chat info", true)]
    public class SetChatInfoViewModel : ViewModelBase, IDataErrorInfo
    {
        #region Properties
        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new RelayCommand(o => SaveChatInfo(), o =>
                        string.IsNullOrEmpty(ValidateAvatar()) && string.IsNullOrEmpty(ValidateTitle()));
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

        private Chat _chat;
        public Chat Chat
        {
            get => _chat;
            set
            {
                if (ReferenceEquals(_chat, value)) return;
                _chat = value;

                _chatInfo = _chat?.Info ?? new ChatInfo();
                AvatarPath = string.Empty;
                OnPropertyChanged(nameof(Chat));
                OnPropertyChanged(nameof(CurrentAvatar));
                OnPropertyChanged(nameof(Title));
            }
        }

        private ChatInfo _chatInfo;

        public byte[] CurrentAvatar => _chat?.Info?.Avatar;

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

        public string Title
        {
            get => _chatInfo.Title;
            set
            {
                if (_chatInfo.Title == value) return;
                _chatInfo.Title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
        #endregion

        #region Constructors
        public SetChatInfoViewModel() { }

        public SetChatInfoViewModel(Chat chat)
        {
            Chat = chat;
        }
        #endregion

        #region Validation
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

        private string ValidateTitle()
        {
            if (string.IsNullOrEmpty(Title))
                return "Title is way too short for sure";
            if (Title.Length > 20)
                return "Title too long!";
            return null;
        }

        public string this[string propName]
        {
            get
            {
                string error;
                switch (propName)
                {
                    case nameof(Title):
                        error = ValidateTitle();
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

        public string Error => string.Empty;
        #endregion

        #region Methods
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

        private void SaveChatInfo()
        {
#if DEBUG
            if (App.IsDesignMode) return;
#endif
            _chatInfo.Avatar = string.IsNullOrEmpty(AvatarPath)
                ? CurrentAvatar
                : Image.FromFile(AvatarPath).Resize(60, 60, false).ToBytes();
            ClientApi.ChatsClient.SetChatInfoAsync(_chat.Id, _chatInfo);
            CloseCommand.Execute(null);
        }
        #endregion 
    }
}