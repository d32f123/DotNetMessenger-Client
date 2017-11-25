using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.RClient;
using DotNetMessenger.WPFClient.ViewModels.Entities;

namespace DotNetMessenger.WPFClient.ViewModels
{
    public class AllChatsViewModel : ViewModelBase
    {
        public ObservableCollection<UserViewModel> Users { get; set; } = new ObservableCollection<UserViewModel>();
        public ObservableCollection<ChatViewModel> Chats { get; set; } = new ObservableCollection<ChatViewModel>();
        public ObservableCollection<HistoryViewModel> Histories { get; set; } =
            new ObservableCollection<HistoryViewModel>();

        private EntityViewModel _currentModel;
        public EntityViewModel CurrentModel
        {
            get => _currentModel;
            set
            {
                if (_currentModel == value) return;
                _currentModel = value;
                OnPropertyChanged(nameof(CurrentModel));
                CurrentChatChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<EntityViewModel> CurrentChatChanged;

        public AllChatsViewModel()
        {
#if DEBUG
            if (App.IsDesignMode) return;
#endif
            Task.Run(async () =>
            {
                await AddNewUsers(await ClientApi.UsersClient.GetAllUsersAsync());
                await AddNewChats(
                    (await ClientApi.ChatsClient.GetUserChatsAsync()).Where(x => x.ChatType == ChatTypes.GroupChat));
                ClientApi.UsersClient.NewUsersEvent += UsersClientOnNewUsersEvent;
                ClientApi.ChatsClient.NewChatsEvent += ChatsClientOnNewChatsEvent;
            });
        }

        private async Task AddNewUsers(IEnumerable<User> users)
        {
            var list = new List<(User, Chat, Message)>();
            foreach (var user in users)
            {
                var chat = await ClientApi.ChatsClient.GetDialogChatAsync(user.Id);
                var message = await ClientApi.MessagesClient.GetLatestChatMessageAsync(chat.Id);
                list.Add((user, chat, message));
            }

            Application.Current.Dispatcher.Invoke(() => list.ForEach(x =>
            {
                var (user, chat, message) = x;
                Users.Add(new UserViewModel(user));
                if (message == null)
                    ClientApi.MessagesClient.SubscribeToChat(chat.Id, -1, NewMessageReceivedEvent);
                else
                    Application.Current.Dispatcher.Invoke(() => Histories.Add(new HistoryViewModel(user)));
            }));
        }

        private async Task AddNewChats(IEnumerable<Chat> chats)
        {
            foreach (var chat in chats)
            {
                Application.Current.Dispatcher.Invoke(() => Chats.Add(new ChatViewModel(chat)));
                var lastMessage = await ClientApi.MessagesClient.GetLatestChatMessageAsync(chat.Id);
                if (lastMessage == null)
                    ClientApi.MessagesClient.SubscribeToChat(chat.Id, -1, NewMessageReceivedEvent);
                else
                    Application.Current.Dispatcher.Invoke(() => Histories.Add(new HistoryViewModel(chat)));
            }            
        }

        private void ChatsClientOnNewChatsEvent(object sender, IEnumerable<Chat> enumerable)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var chat in enumerable)
                {
                    Chats.Add(new ChatViewModel(chat));
                    ClientApi.MessagesClient.SubscribeToChat(chat.Id, -1, NewMessageReceivedEvent);
                }
            });
        }

        private void UsersClientOnNewUsersEvent(object sender, IEnumerable<User> enumerable)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                foreach (var user in enumerable)
                {
                    Users.Add(new UserViewModel(user));
                    var chat = await ClientApi.ChatsClient.GetDialogChatAsync(user.Id);
                    ClientApi.MessagesClient.SubscribeToChat(chat.Id, -1, NewMessageReceivedEvent);
                }
            });
        }

        private async void NewMessageReceivedEvent(object sender, IEnumerable<Message> enumerable)
        {
            foreach (var msg in enumerable)
            {
                ClientApi.MessagesClient.UnsubscribeFromChat(msg.ChatId, NewMessageReceivedEvent);
                var chat = await ClientApi.ChatsClient.GetChatAsync(msg.ChatId);
                if (chat.ChatType == ChatTypes.GroupChat)
                {
                    Application.Current.Dispatcher.Invoke(() => Histories.Add(new HistoryViewModel(chat)));
                    continue;
                }
                var userId = chat.Users.SingleOrDefault(x => x != ClientApi.UserId);
                // handle the case where dialog is between one and the same user
                if (userId == default) userId = ClientApi.UserId;
                var user = await ClientApi.UsersClient.GetUserAsync(userId);
                Application.Current.Dispatcher.Invoke(() => Histories.Add(new HistoryViewModel(user)));
            }
        }
    }
}