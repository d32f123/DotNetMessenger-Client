using System;
using DotNetMessenger.Model;

namespace DotNetMessenger.WPFClient.ViewModels.Entities
{
    public class ChatViewModel : EntityViewModel
    {
        private Chat _currentChat;
        public Chat CurrentChat
        {
            get => _currentChat;
            set
            {
                if (value == _currentChat) return;
                _currentChat = value;
                OnPropertyChanged(nameof(CurrentChat));
                OnPropertyChanged(nameof(MainString));
                OnPropertyChanged(nameof(SecondaryString));
                OnPropertyChanged(nameof(Image));
            }
        }

        public ChatViewModel()
        {
            _currentChat = new Chat();
        }

        public ChatViewModel(Chat currentChat)
        {
            _currentChat = currentChat;
        }

        public override string MainString => _currentChat.Info?.Title ?? _currentChat.Id.ToString();
        public override string SecondaryString => string.Empty;
        public override byte[] Image => _currentChat.Info?.Avatar;
        public override DateTime Date => DateTime.MinValue;
    }
}