using System;
using System.Collections.ObjectModel;
using DotNetMessenger.Model;
using DotNetMessenger.WPFClient.Router;
using DotNetMessenger.WPFClient.ViewModels.Info;

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

        public ChatViewModel() : this(new Chat())
        {
        }

        public ChatViewModel(Chat currentChat)
        {
            ContextActions = new ObservableCollection<ContextAction>
            {
                new ContextAction {Name = "Info", Action = new RelayCommand(ShowInfoViewModel)}
            };
            _currentChat = currentChat;
        }

        public override string MainString => _currentChat.Info?.Title ?? _currentChat.Id.ToString();
        public override string SecondaryString => string.Empty;
        public override byte[] Image => _currentChat.Info?.Avatar;
        public override DateTime Date => DateTime.MinValue;

        public sealed override ObservableCollection<ContextAction> ContextActions { get; set; }
            

        private void ShowInfoViewModel(object o)
        {
            ViewHostBuilder.GetViewHost().HostView(new ChatInfoViewModel(_currentChat));
        }
    }
}