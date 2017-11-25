﻿using System;
using System.Collections.ObjectModel;
using System.Windows;
using DotNetMessenger.Model;
using DotNetMessenger.RClient;

namespace DotNetMessenger.WPFClient.ViewModels
{
    public class MessageViewModel : ViewModelBase
    {
        private User _user;
        private Message _message;

        public User User
        {
            get => _user;
            set
            {
                if (_user?.Id == value?.Id) return;
                _user = value;
                OnPropertyChanged(nameof(User));
                OnPropertyChanged(nameof(Avatar));
                OnPropertyChanged(nameof(SenderName));
            }
        }

        public Message Message
        {
            get => _message;
            set
            {
                if (_message?.Id == value?.Id) return;
                User = null;
                _message = value;
                OnPropertyChanged(nameof(MessageText));
                OnPropertyChanged(nameof(MessageDate));

                if (_message == null) return;

                AttacmentViewModels.Clear();
                if (_message.Attachments != null)
                    foreach (var attachment in _message.Attachments)
                    {
                        AttacmentViewModels.Add(new AttachmentViewModel(attachment));
                    }
                
                OnPropertyChanged(nameof(AttacmentViewModels));
                Application.Current.Dispatcher.Invoke(async () =>
                    User = await ClientApi.UsersClient.GetUserAsync(_message.SenderId));
            }
        }

        public byte[] Avatar => _user?.UserInfo?.Avatar;
        public string SenderName => _user?.Username;
        public string MessageText => _message?.Text;
        public DateTime MessageDate => _message?.Date ?? DateTime.MinValue;

        public ObservableCollection<AttachmentViewModel> AttacmentViewModels { get; set; } =
            new ObservableCollection<AttachmentViewModel>();

        public MessageViewModel()
        {}

        public MessageViewModel(Message message)
        {
            Message = message;
        }
    }
}