using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.RClient;
using DotNetMessenger.WPFClient.Extensions;

namespace DotNetMessenger.WPFClient.ViewModels
{
    public class SenderViewModel : ViewModelBase
    {
        private RolePermissions _permissions;
        private Chat _chat;
        private string _messageText;
        private Attachment _attachment;
        private int _expirationSeconds;

        private ICommand _sendCommand;
        private ICommand _attachCommand;
        private ICommand _unattachCommand;

        public Chat Chat
        {
            get => _chat;
            set
            {
                if (Equals(_chat, value)) return;
                if (_chat?.ChatType == ChatTypes.Dialog)
                {
                    ClientApi.ChatsClient.UnsubscribeFromNewChatUserInfo(_chat.Id, ClientApi.UserId, NewChatUserInfo);
                }
                _chat = value;
                MessageText = string.Empty;
                Attachment = null;
                ExpirationSeconds = 0;
                _permissions = RolePermissions.NaN;
                OnPropertyChanged(nameof(Permissions));

                if (value == null) return;
#if DEBUG
                if (App.IsDesignMode) return;
#endif
                if (_chat.ChatType == ChatTypes.Dialog)
                {
                    _permissions = RolePermissions.WritePerm | RolePermissions.AttachPerm;
                }
                else
                {
                    ClientApi.ChatsClient.SubscribeToNewChatUserInfo(_chat.Id, ClientApi.UserId, NewChatUserInfo);
                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        _permissions = (await ClientApi.ChatsClient.GetChatUserInfoAsync(_chat.Id)).Role
                            .RolePermissions;
                        OnPropertyChanged(nameof(Permissions));
                    });
                }
                OnPropertyChanged(nameof(Chat));
            }
        }

        private void NewChatUserInfo(object sender, ChatUserInfo chatUserInfo)
        {
            _permissions = chatUserInfo.Role.RolePermissions;
            OnPropertyChanged(nameof(Permissions));
        }

        public RolePermissions Permissions => _permissions;

        public string MessageText
        {
            get => _messageText;
            set
            {
                if (_messageText == value) return;
                _messageText = value;
                OnPropertyChanged(nameof(MessageText));
            }
        }
       
        public Attachment Attachment
        {
            get => _attachment;
            set
            {
                if (_attachment == value) return;
                _attachment = value;
                OnPropertyChanged(nameof(Attachment));
                OnPropertyChanged(nameof(IsFileAttached));
            }
        }

        public bool IsFileAttached => _attachment != null;
        
        public int ExpirationSeconds
        {
            get => _expirationSeconds;
            set
            {
                if (_expirationSeconds == value) return;
                _expirationSeconds = value;
                OnPropertyChanged(nameof(ExpirationSeconds));
            }
        }
        
        public ICommand SendCommand
        {
            get
            {
                if (_sendCommand == null)
                {
                    _sendCommand = new RelayCommand(async o => await SendMessageAsync(), o => CheckSendPermissions());
                }
                return _sendCommand;
            }
        }

        public ICommand AttachCommand
        {
            get
            {
                if (_attachCommand == null)
                {
                    _attachCommand = new RelayCommand(o => AttachFile(),
                        o => (_permissions & RolePermissions.AttachPerm) != 0);
                }
                return _attachCommand;
            }
        }

        public ICommand UnattachCommand
        {
            get
            {
                if (_unattachCommand == null)
                {
                    _unattachCommand = new RelayCommand(o => Attachment = null);
                }
                return _unattachCommand;
            }
        }
        
        private async Task SendMessageAsync()
        {
            var text = _messageText;
            var attachment = _attachment;
            var expirationSeconds = _expirationSeconds;
            MessageText = null;
            Attachment = null;
            ExpirationSeconds = 0;
            await ClientApi.MessagesClient.SendMessageAsync(Chat.Id, new Message
            {
                ChatId = _chat.Id,
                SenderId = ClientApi.UserId,
                Text = text,
                Date = DateTime.Now,
                ExpirationDate = expirationSeconds == 0 ? null : (DateTime?)DateTime.Now.AddSeconds(expirationSeconds),
                Attachments = attachment != null ? new List<Attachment> { attachment } : null
            });
        }

        private bool CheckSendPermissions()
        {
            if (_permissions == RolePermissions.NaN) return false;
            if ((_permissions & RolePermissions.WritePerm) == 0) return false;
            if (_attachment != null && (_permissions & RolePermissions.AttachPerm) == 0) return false;
            return true;
        }

        private void AttachFile()
        {
            var filename = "";
            if (!FileDialogHelpers.GetImageFromDialog(ref filename)) return;
            var length = new System.IO.FileInfo(filename).Length;
            var shortFileName = new System.IO.FileInfo(filename).Name;
            // if length > 30 MB = 30 MB * 1024 kb/mb * 1024 b/kb
            if (length > 30 * 1024 * 1024)
            {
                MessageBox.Show("This file is way too large! Please something up to 30 mb only", "File too big",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Now load the file
            var isImage = true;
            Image image;
            try
            {
                image = Image.FromFile(filename);
            }
            catch
            {
                // not an image for sure!
                isImage = false;
                image = null;
            }
            if (isImage)
            {
                Attachment = new Attachment { Type = AttachmentTypes.Image, File = image.ToBytes(), FileName = shortFileName };
            }
            else
            {
                Attachment = new Attachment
                {
                    Type = AttachmentTypes.RegularFile,
                    FileName = shortFileName,
                    File = System.IO.File.ReadAllBytes(filename)
                };
            }
        }
    }
}