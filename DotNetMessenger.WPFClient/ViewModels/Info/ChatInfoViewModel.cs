using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using DotNetMessenger.Model;
using DotNetMessenger.RClient;
using DotNetMessenger.WPFClient.Router;

namespace DotNetMessenger.WPFClient.ViewModels.Info
{
    [WindowSettings("Chat info", true)]
    public class ChatInfoViewModel : ViewModelBase
    {
        private Chat _chat;
        public Chat Chat
        {
            get => _chat;
            set
            {
                if (ReferenceEquals(_chat, value)) return;
                _chat = value;
                UsersInfo.Clear();
                OnPropertyChanged(nameof(Chat));
                OnPropertyChanged(nameof(Id));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Avatar));
                OnPropertyChanged(nameof(UsersTotal));

                if (_chat == null) return;

                new Thread(() =>
                {
                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        var chatUsers = (await ClientApi.ChatsClient.GetChatAsync(_chat.Id)).Users;
                        foreach (var userId in chatUsers)
                        {
                            var user = await ClientApi.UsersClient.GetUserAsync(userId);
                            UsersInfo.Add(new ChatUserEntryViewModel(_chat.Id, user));
                        }
                    });
                }).Start();
            }
        }

        public int Id => _chat?.Id ?? -1;
        public string Title => _chat?.Info?.Title;
        public byte[] Avatar => _chat?.Info?.Avatar;
        public int UsersTotal => _chat?.Users?.Count() ?? -1;

        public ObservableCollection<ChatUserEntryViewModel> UsersInfo { get; set; } = new ObservableCollection<ChatUserEntryViewModel>();

        public ChatInfoViewModel()
        {
            Chat = null;
        }

        public ChatInfoViewModel(Chat chat)
        {
            Chat = chat;
        }
    }
}