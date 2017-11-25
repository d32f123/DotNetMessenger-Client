using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.RClient.Interfaces;
using DotNetMessenger.RClient.LongPollers;
using DotNetMessenger.RClient.Storages;

namespace DotNetMessenger.RClient.Clients
{
    public class ChatsClient : IChatsClient
    {
        private readonly HttpClient _client;
        private readonly IUsersClient _usersClient;
        private int UserId => _usersClient.UserId;

        private readonly NewChatPoller _chatPoller;
        public event EventHandler<IEnumerable<Chat>> NewChatsEvent;

        private readonly CacheStorage<int, Chat> _chatCache = new CacheStorage<int, Chat>(100);
        private readonly CacheStorage<int, int> _dialogCache = new CacheStorage<int, int>(50);
        private readonly CacheStorage<ChatUserIds, ChatUserInfo> _infoCache =
            new CacheStorage<ChatUserIds, ChatUserInfo>(100);
        private int _chatsTotal;

        private class ChatUserIds
        {
            public readonly int UserId;
            public readonly int ChatId;

            public ChatUserIds(int userId, int chatId)
            {
                UserId = userId;
                ChatId = chatId;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is ChatUserIds other)) return false;
                return other.UserId == UserId && other.ChatId == ChatId;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (UserId * 397) ^ ChatId;
                }
            }
        }

        public ChatsClient(string connectionString, Guid token, IUsersClient usersClient)
        {
            _usersClient = usersClient;

            _client = new HttpClient { BaseAddress = new Uri(connectionString) };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{token}:".ToBase64String());

            _chatsTotal = -1;
            var chats = GetUserChatsAsync().Result;
           

            _chatCache.AddRange(chats.Select(x => new KeyValuePair<int, Chat>(x.Id, x)));
            foreach (var chat in chats.Where(x => x.ChatType == ChatTypes.Dialog))
            {
                var users = chat.Users.ToArray();
                var otherUser = users.Length == 1 ? users[0] : users.Single(x => x != UserId);
                _dialogCache.Add(otherUser, chat.Id);
            }
            _chatsTotal = chats.Count;

            NewChatsEvent += OnNewChatsEvent;

            _chatPoller = new NewChatPoller(connectionString, token, -1, (_, e) => NewChatsEvent?.Invoke(this, e));
        }

        private void OnNewChatsEvent(object sender, IEnumerable<Chat> enumerable)
        {
            var chats = enumerable as Chat[] ?? enumerable.ToArray();
            _chatsTotal += chats.Length;
            _chatCache.AddRange(chats.Select(x => new KeyValuePair<int, Chat>(x.Id, x)));

            foreach (var chat in chats.Where(x => x.ChatType == ChatTypes.Dialog))
            {
                var users = chat.Users.ToArray();
                var otherUser = users.Length == 1 ? users[0] : users.Single(x => x != UserId);
                _dialogCache.Add(otherUser, chat.Id);
            }

        }

        public async Task<List<Chat>> GetUserChatsAsync()
        {
            if (_chatsTotal == _chatCache.Count)
            {
                var chatsCached = _chatCache.Values;
                return chatsCached as List<Chat> ?? chatsCached.ToList();
            }

            var response = await _client.GetAsync($"users/{UserId}/chats").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadAsAsync<List<Chat>>();
        }

        public async Task<Chat> GetChatAsync(int chatId)
        {
            if (_chatCache.ContainsKey(chatId)) return _chatCache[chatId];

            var response = await _client.GetAsync($"chats/{chatId}").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) return null;

            var ret = await response.Content.ReadAsAsync<Chat>();
            _chatCache.Add(chatId, ret);
            return ret;
        }

        public async Task<Chat> GetDialogChatAsync(int otherId)
        {
            if (_dialogCache.ContainsKey(otherId))
            {
                var chatId = _dialogCache[otherId];
                if (_chatCache.ContainsKey(chatId)) return _chatCache[chatId];
            }

            var response = await _client.GetAsync($"chats/dialog/{UserId}/{otherId}").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var ret = await response.Content.ReadAsAsync<Chat>();
                _chatCache.Add(ret.Id, ret);
                _dialogCache.Add(otherId, ret.Id);
            }
            return null;
        }

        public async Task<ChatUserInfo> GetChatUserInfoAsync(int chatId)
        {
            return await GetChatSpecificUserinfoAsync(chatId, UserId);
        }

        public async Task<ChatUserInfo> GetChatSpecificUserinfoAsync(int chatId, int userId)
        {
            var chatUserId = new ChatUserIds(userId, chatId);
            if (_infoCache.ContainsKey(chatUserId)) return _infoCache[chatUserId];

            var response = await _client.GetAsync($"chats/{chatId}/users/{userId}/info").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) return null;

            var ret = await response.Content.ReadAsAsync<ChatUserInfo>();
            _infoCache.Add(chatUserId, ret);
            return ret;
        }

        public async Task<Chat> CreateNewGroupChat(string chatName, IEnumerable<int> users)
        {
            var userList = users as List<int> ?? users.ToList();
            userList.Insert(0, UserId);
            var response = await _client
                .PostAsJsonAsync("chats/", new ChatCredentials { ChatType = ChatTypes.GroupChat, Title = chatName, Members = userList })
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<Chat>();
            }
            return null;
        }

        private class ChatCredentials
        {
            public ChatTypes ChatType { get; set; }
            public string Title { get; set; }
            public IEnumerable<int> Members { get; set; }
        }

        public void Dispose()
        {
            _client?.Dispose();
            _chatPoller?.Dispose();
        }
    }
}