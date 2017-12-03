using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.RClient.Classes;
using DotNetMessenger.RClient.Extensions;
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

        private readonly object _lock = new object();
        private NewEventsPoller _poller;

        public NewEventsPoller Poller
        {
            set
            {
                lock (_lock)
                {
                    if (_poller == value) return;
                    if (_poller != null)
                    {
                        _poller.NewChatsEvent -= PollerOnNewChatsEvent;
                        _poller.LostChatsEvent -= PollerOnLostChatsEvent;
                    }

                    _poller = value;
                    _poller.NewChatsEvent += PollerOnNewChatsEvent;
                    _poller.LostChatsEvent += PollerOnLostChatsEvent;
                }
            }
        }

        private void PollerOnLostChatsEvent(object sender, IEnumerable<int> enumerable)
        {
            LostChatsEvent?.Invoke(this, enumerable);
        }

        private void PollerOnNewChatsEvent(object sender, IEnumerable<Chat> enumerable)
        {
            NewChatsEvent?.Invoke(this, enumerable);
        }

        public event EventHandler<IEnumerable<Chat>> NewChatsEvent;
        public event EventHandler<IEnumerable<int>> LostChatsEvent;

        private readonly CacheStorage<int, int> _dialogCache = new CacheStorage<int, int>(50);

        public ChatsClient(string connectionString, Guid token, IUsersClient usersClient)
        {
            _usersClient = usersClient;

            _client = new HttpClient { BaseAddress = new Uri(connectionString) };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{token}:".ToBase64String());
        }

        public async Task<List<Chat>> GetUserChatsAsync()
        {
            lock (_lock)
                if (_poller != null)
                    if (_poller.AreChatsPolled)
                    {
                        var chatsCached = _poller.ChatsStorage.Values.Select(x => x);
                        return chatsCached as List<Chat> ?? chatsCached.ToList();
                    }

            var response = await _client.GetAsync($"users/{UserId}/chats").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;

            var chats = await response.Content.ReadAsAsync<List<Chat>>();

            return chats;
        }

        public async Task<Chat> GetChatAsync(int chatId)
        {
            lock (_lock)
                if (_poller != null)
                    if (_poller.ChatsStorage.ContainsKey(chatId))
                        return _poller.ChatsStorage[chatId];

            var response = await _client.GetAsync($"chats/{chatId}").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) return null;

            var ret = await response.Content.ReadAsAsync<Chat>();
            return ret;
        }

        public async Task<Chat> GetDialogChatAsync(int otherId)
        {
            if (_dialogCache.ContainsKey(otherId))
            {
                var chatId = _dialogCache[otherId];
                lock (_lock)
                    if (_poller != null)
                        lock (_poller.ChatsStorage)
                            if (_poller.ChatsStorage.ContainsKey(chatId)) return _poller.ChatsStorage[chatId];
            }

            var response = await _client.GetAsync($"chats/dialog/{UserId}/{otherId}").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var ret = await response.Content.ReadAsAsync<Chat>();
                if (!_dialogCache.ContainsKey(otherId))
                    _dialogCache.Add(otherId, ret.Id);
                return ret;
            }
            return null;
        }

        public async Task AddUsersAsync(int chatId, IEnumerable<int> userIds)
        {
            await _client.PostAsJsonAsync($"chats/{chatId}/users", userIds);
        }

        public async Task KickUsersAsync(int chatId, IEnumerable<int> userIds)
        {
            await _client.PostAsJsonAsync($"chats/{chatId}/users/delete", userIds);
        }

        public async Task<ChatUserInfo> GetChatUserInfoAsync(int chatId)
        {
            return await GetChatSpecificUserInfoAsync(chatId, UserId);
        }

        public async Task<ChatUserInfo> GetChatSpecificUserInfoAsync(int chatId, int userId)
        {
            var chatUserId = new ChatUserId(userId, chatId);
            lock (_lock)
                if (_poller != null)
                    lock (_poller.ChatUserInfosCache)
                        if (_poller.ChatUserInfosCache.ContainsKey(chatUserId))
                            return _poller.ChatUserInfosCache[chatUserId];

            var response = await _client.GetAsync($"chats/{chatId}/users/{userId}/info").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) return null;

            var ret = await response.Content.ReadAsAsync<ChatUserInfo>();
            lock (_lock)
                if (_poller != null)
                    lock (_poller.ChatUserInfosCache)
                        if (!_poller.ChatUserInfosCache.ContainsKey(chatUserId))
                            _poller.ChatUserInfosCache.Add(chatUserId, ret);
            return ret;
        }

        public async Task SetChatSpecificUserInfoAsync(int chatId, int userId, ChatUserInfo info)
        {
            var response = await _client.PutAsJsonAsync($"chats/{chatId}/users/{userId}/info", info);

            if (!response.IsSuccessStatusCode) return;

            lock (_lock)
                _poller?.SetChatUserInfo(userId, chatId, info);
        }

        public async Task SetChatSpecificUserRoleAsync(int chatId, int userId, UserRoles role)
        {
            var roleId = (int) role;
            await _client.PutAsync($"chats/{chatId}/users/{userId}/info/role/{roleId}", null);
        }

        public async Task<bool> SetChatInfoAsync(int chatId, ChatInfo chatInfo)
        {
            var response = await _client.PutAsJsonAsync($"chats/{chatId}/info", chatInfo);

            lock (_lock)
                if (_poller != null)
                    lock (_poller.ChatsStorage)
                        if (response.IsSuccessStatusCode && _poller.UsersStorage.ContainsKey(UserId))
                            _poller.SetChatInfo(chatId, chatInfo);

            return response.IsSuccessStatusCode;
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

        public void SubscribeToNewChatInfo(int chatId, EventHandler<Chat> handler)
        {
            lock (_lock)
                if (_poller == null)
                {
                    Trace.WriteLine("COULD NOT SUBSCRIBE BECUASE POLLER WAS NOT STARTED");
                }
                else
                {
                    _poller.SubscribeToNewChatInfo(chatId, handler);
                }
        }

        public void UnsubscribeFromNewChatInfo(int chatId, EventHandler<Chat> handler)
        {
            lock (_lock)
            {
                _poller?.UnsubscribeFromNewChatInfo(chatId, handler);
            }
        }

        public void SubscribeToNewChatUserInfo(int chatId, int userId, EventHandler<ChatUserInfo> handler)
        {
            lock (_lock)
            {
                _poller.SubscribeToNewChatUserInfo(chatId, userId, handler);
            }
        }

        public void UnsubscribeFromNewChatUserInfo(int chatId, int userId, EventHandler<ChatUserInfo> handler)
        {
            lock (_lock)
            {
                _poller.UnsubscribeFromNewChatUserInfo(chatId, userId, handler);
            }
        }

        public void SubscribeToNewChatMembers(int chatId, EventHandler<IEnumerable<int>> handler)
        {
            lock (_lock)
            {
                _poller.SubscribeToNewChatMembers(chatId, handler);
            }
        }

        public void UnsubscribeFromNewChatMembers(int chatId, EventHandler<IEnumerable<int>> handler)
        {
            lock (_lock)
            {
                _poller.UnsubscribeFromNewChatMembers(chatId, handler);
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}