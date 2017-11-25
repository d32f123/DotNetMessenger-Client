using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using DotNetMessenger.Model;
using DotNetMessenger.RClient;
using DotNetMessenger.WPFClient.Router;
using DotNetMessenger.WPFClient.ViewModels.Info;

namespace DotNetMessenger.WPFClient.ViewModels.Entities
{
    public class HistoryViewModel : EntityViewModel, IDisposable
    {
        private User _user;
        private Chat _chat;

        private bool _isUser;
        private int _chatId = -1;
        private byte[] _avatar;
        private string _title;

        public int ChatId => _chatId;

        public Chat Chat
        {
            set
            {
                if (_chatId == (value?.Id ?? -1)) return;
                if (_chatId != -1)
                    ClientApi.MessagesClient.UnsubscribeFromChat(_chatId, NewMessagesHandler);
                _chatId = value?.Id ?? -1;
                _avatar = value?.Info?.Avatar;
                _title = value?.Info?.Title ?? value?.Id.ToString();
                LastMessage = null;
                _user = null;
                _chat = value;
                _isUser = false;

                OnPropertyChanged(nameof(MainString));
                OnPropertyChanged(nameof(SecondaryString));
                OnPropertyChanged(nameof(Image));
                OnPropertyChanged(nameof(Date));

                if (value == null) return;

                Application.Current.Dispatcher.Invoke(async () =>
                {
                    LastMessage = await ClientApi.MessagesClient.GetLatestChatMessageAsync(_chatId);
                    ClientApi.MessagesClient.SubscribeToChat(_chatId, LastMessage?.Id ?? -1, NewMessagesHandler
                    );
                });
            }
        }

        private void NewMessagesHandler(object sender, IEnumerable<Message> enumerable)
        {
            LastMessage = enumerable.Last();
        }

        public User User
        {
            set
            {
                if (_chatId != -1)
                    ClientApi.MessagesClient.UnsubscribeFromChat(_chatId, NewMessagesHandler);
                _chatId = -1;
                _avatar = value?.UserInfo?.Avatar;
                _title = value?.Username ?? string.Empty;
                LastMessage = null;
                _user = value;
                _chat = null;
                _isUser = true;

                OnPropertyChanged(nameof(MainString));
                OnPropertyChanged(nameof(SecondaryString));
                OnPropertyChanged(nameof(Image));
                OnPropertyChanged(nameof(Date));

                if (value == null) return;

                Application.Current.Dispatcher.Invoke(async () =>
                {
                    _chatId = (await ClientApi.ChatsClient.GetDialogChatAsync(value.Id)).Id;
                    LastMessage = await ClientApi.MessagesClient.GetLatestChatMessageAsync(_chatId);
                    ClientApi.MessagesClient.SubscribeToChat(_chatId, LastMessage?.Id ?? -1, NewMessagesHandler);
                });
            }
        }

        private Message _previousMessage;
        private Message _lastMessage;
        public Message LastMessage
        {
            get => _lastMessage;
            private set
            {
                if (_lastMessage == value) return;
                _previousMessage = _lastMessage;
                _lastMessage = value;
                OnPropertyChanged(nameof(SecondaryString));
                OnPropertyChanged(nameof(Date));

                new Thread(ExpirableMessageChecker) { IsBackground = true }.Start();
            }
        }

        private void ExpirableMessageChecker()
        {
            while (true)
            {
                if (_lastMessage?.ExpirationDate == null) return;
                if (DateTime.Compare((DateTime)_lastMessage.ExpirationDate, DateTime.Now) < 0)
                {
                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        // if message has expired, check if the previous message has expired as well
                        // if yes, we need to get new message from the server
                        // else set LastMessage to previous message
                        if (_previousMessage == null || _previousMessage.ExpirationDate.HasValue &&
                            DateTime.Compare((DateTime) _previousMessage.ExpirationDate, DateTime.Now) < 0)
                        {
                            // previous message has expired as well, get message from server
                            LastMessage = await ClientApi.MessagesClient.GetLatestChatMessageAsync(_chatId);
                            _previousMessage = null;
                        }
                        else
                        {
                            LastMessage = _previousMessage;
                            _previousMessage = null;
                        }
                    });
                    return;
                }
                Thread.Sleep(1000);
            }
        }

        public HistoryViewModel()
        {
            ContextActions = new ObservableCollection<ContextAction>
            {
                new ContextAction {Name = "Info", Action = new RelayCommand(ShowInfoViewModel)}
            };
        }

        private void ShowInfoViewModel(object o)
        {
            if (!_isUser)
                ViewHostBuilder.GetViewHost().HostView(new ChatInfoViewModel(_chat));
            else
            {
                ViewHostBuilder.GetViewHost().HostView(new UserInfoViewModel(_user));
            }
        }

        public sealed override ObservableCollection<ContextAction> ContextActions { get; set; }

        public HistoryViewModel(Chat chat)
        {
            Chat = chat;
        }

        public HistoryViewModel(User user)
        {
            User = user;
        }

        public override string MainString => _title;
        public override string SecondaryString
        {
            get
            {
                if (_lastMessage == null) return string.Empty;
                if (string.IsNullOrEmpty(_lastMessage.Text))
                {
                    // if there is no text, there should be a file attached
                    return "<attachment>";
                }
                if (_lastMessage.Text.Length > 15)
                {
                    return _lastMessage.Text.Substring(0, 15) + "...";
                }
                return _lastMessage.Text;
            }
        }

        public override byte[] Image => _avatar;
        public override DateTime Date => _lastMessage?.Date ?? DateTime.MinValue;

        public void Dispose()
        {
            if (_chatId != -1)
                ClientApi.MessagesClient.UnsubscribeFromChat(_chatId, NewMessagesHandler);
        }
    }
}