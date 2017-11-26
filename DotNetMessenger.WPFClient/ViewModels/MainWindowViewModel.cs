using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DotNetMessenger.Model;
using DotNetMessenger.RClient;
using DotNetMessenger.WPFClient.Router;
using DotNetMessenger.WPFClient.ViewModels.Entities;
using DotNetMessenger.WPFClient.ViewModels.Info;

namespace DotNetMessenger.WPFClient.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private AllChatsViewModel _allChatsViewModel;
        private int _currentChat;
        private UserViewModel _currentUser;
        private SenderViewModel _senderViewModel;
        private ICommand _createGroupChatCommand;
        private ICommand _setNewUserInfoCommand;

        public AllChatsViewModel AllChatsViewModel
        {
            get => _allChatsViewModel;
            set
            {
                if (_allChatsViewModel == value) return;
                _allChatsViewModel = value;
                OnPropertyChanged(nameof(AllChatsViewModel));
            }
        }

        public ObservableCollection<MessageViewModel> CurrentMessages { get; set; } =
            new ObservableCollection<MessageViewModel>();
        
        public UserViewModel CurrentUser
        {
            get => _currentUser;
            set
            {
                if (_currentUser == value) return;
                _currentUser = value;
                OnPropertyChanged(nameof(CurrentUser));
            }
        }

        public SenderViewModel SenderViewModel
        {
            get => _senderViewModel;
            set
            {
                if (_senderViewModel == value) return;
                _senderViewModel = value;
                OnPropertyChanged(nameof(SenderViewModel));
            }
        }

        public ICommand CreateGroupChatCommand
        {
            get
            {
                if (_createGroupChatCommand == null)
                {
                    _createGroupChatCommand = new RelayCommand(x =>
                        ViewHostBuilder.GetViewHost()
                            .HostViewModal(new NewChatViewModel(_allChatsViewModel.Users.Select(u => u.CurrentUser)
                                .Where(u => u.Id != ClientApi.UserId))));
                }
                return _createGroupChatCommand;
            }
        }

        public ICommand SetNewUserInfoCommand
        {
            get
            {
                if (_setNewUserInfoCommand == null)
                {
                    _setNewUserInfoCommand = new RelayCommand(x =>
                        ViewHostBuilder.GetViewHost()
                            .HostViewModal(new SetUserInfoViewModel(CurrentUser.CurrentUser)));
                }
                return _setNewUserInfoCommand;
            }
        }

        public MainWindowViewModel()
        {
            AllChatsViewModel = new AllChatsViewModel();
#if DEBUG
            if (App.IsDesignMode) return;
#endif
            AllChatsViewModel.CurrentChatChanged += OnCurrentChatChanged; 
            Application.Current.Dispatcher.Invoke(async () =>
                CurrentUser = new UserViewModel(await ClientApi.UsersClient.GetUserAsync(ClientApi.UserId)));
            _currentChat = -1;
            SenderViewModel = new SenderViewModel();
            new Thread(ExpiredMessagesDeleter) {IsBackground = true}.Start();
        }

        private async void OnCurrentChatChanged(object sender, EntityViewModel entityViewModel)
        {
            CurrentMessages.Clear();
            SenderViewModel.Chat = null;
            if (_currentChat != -1)
                ClientApi.MessagesClient.UnsubscribeFromChat(_currentChat, OnNewMessages);
            switch (entityViewModel)
            {
                case UserViewModel uvm:
                {
                    var chat = await ClientApi.ChatsClient.GetDialogChatAsync(uvm.CurrentUser.Id);
                    await SetNewChat(chat);
                    break;
                }
                case ChatViewModel cvm:
                {
                    await SetNewChat(cvm.CurrentChat);
                    break;
                }
                case HistoryViewModel hvm:
                {
                    await SetNewChat(await ClientApi.ChatsClient.GetChatAsync(hvm.ChatId));
                    break;
                }
            }
        }

        private async Task SetNewChat(Chat chat)
        {
            var chatId = chat.Id;
            _currentChat = chatId;
            SenderViewModel.Chat = chat;
            OnNewMessages(this, await ClientApi.MessagesClient.GetChatMessagesAsync(chatId));
            ClientApi.MessagesClient.SubscribeToChat(chatId, CurrentMessages.LastOrDefault()?.Message?.Id ?? -1, OnNewMessages);
        }

        private void OnNewMessages(object sender, IEnumerable<Message> enumerable)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var message in enumerable)
                {
                    CurrentMessages.Add(new MessageViewModel(message));
                }
            });
        }

        private void ExpiredMessagesDeleter()
        {
            while (true)
            {
                if (CurrentMessages != null && CurrentMessages.Any())
                {
                    CurrentMessages
                        .Where(x => x.Message.ExpirationDate != null &&
                                    DateTime.Compare((DateTime)x.Message.ExpirationDate, DateTime.Now) < 0)
                        .ToList().ForEach(x => Application.Current.Dispatcher.Invoke(() => CurrentMessages.Remove(x)));
                }
                Thread.Sleep(1000);
            }
        }
    }
}