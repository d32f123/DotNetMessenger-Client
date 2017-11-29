using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetMessenger.Model;
using DotNetMessenger.RClient.Classes;
using DotNetMessenger.RClient.Extensions;
using DotNetMessenger.RClient.Interfaces;
using DotNetMessenger.RClient.Storages;

namespace DotNetMessenger.RClient.LongPollers
{
    public class NewEventsPoller : LongPoller
    {
        public event EventHandler<IEnumerable<User>> NewUsersEvent;
        public event EventHandler<IEnumerable<Chat>> NewChatsEvent;
        public event EventHandler<IEnumerable<Message>> NewMessagesEvent;

        private readonly IUsersClient _usersClient;

        private readonly Dictionary<int, EventHandler<User>> _newUserInfoHandlers;
        private readonly Dictionary<int, EventHandler<Chat>> _newChatInfoHandlers;
        private readonly Dictionary<int, EventHandler<IEnumerable<Message>>> _newChatMessagesHandlers;
        private readonly Dictionary<int, EventHandler<IEnumerable<int>>> _newChatMembersHandlers;
        private readonly Dictionary<ChatUserId, EventHandler<ChatUserInfo>> _newChatUserInfosHandlers; 

        public NewEventsPoller(string connectionString, Guid token, IUsersClient usersClient, IChatsClient chatsClient) 
            : base(connectionString, token)
        {
            AreChatsPolled = false;
            UsersStorage = new Dictionary<int, User>();
            ChatsStorage = new Dictionary<int, Chat>();
            ChatUserInfosCache = new CacheStorage<ChatUserId, ChatUserInfo>();
            _lastChatId = -1;
            _lastUserId = -1;

            _newChatInfoHandlers = new Dictionary<int, EventHandler<Chat>>();
            _newChatMembersHandlers = new Dictionary<int, EventHandler<IEnumerable<int>>>();
            _newChatMessagesHandlers = new Dictionary<int, EventHandler<IEnumerable<Message>>>();
            _newChatUserInfosHandlers = new Dictionary<ChatUserId, EventHandler<ChatUserInfo>>();
            _newUserInfoHandlers = new Dictionary<int, EventHandler<User>>();

            usersClient.GetAllUsersAsync().Result.ForEach(x => UsersStorage.Add(x.Id, x));
            chatsClient.GetUserChatsAsync().Result.ForEach(x => ChatsStorage.Add(x.Id, x));

            _usersClient = usersClient;
        }

        public bool AreChatsPolled { get; set; }

        public class MessageWithState
        {
            public int MessageId { get; set; }
            public bool IsPolled { get; set; }
        }

        private int _lastChatId;
        private int _lastUserId;
        public readonly Dictionary<int, MessageWithState> ChatMessagesDictionary = new Dictionary<int, MessageWithState>();

        public Dictionary<int, User> UsersStorage { get; }
        public Dictionary<int, Chat> ChatsStorage { get; }
        public CacheStorage<ChatUserId, ChatUserInfo> ChatUserInfosCache { get; }

        public void SubscribeToNewChatMessages(int chatId, EventHandler<IEnumerable<Message>> handler)
        {
            lock (ChatsStorage)
            {
                if (!ChatsStorage.ContainsKey(chatId))
                {
                    ChatsStorage.Add(chatId, new Chat { Id = chatId });
                    ChatMessagesDictionary.Add(chatId, new MessageWithState {IsPolled = false, MessageId = -1});
                }
            }
            lock (_newChatMessagesHandlers)
            {
                if (_newChatMessagesHandlers.ContainsKey(chatId))
                {
                    _newChatMessagesHandlers[chatId] += handler;
                }
                else
                {
                    _newChatMessagesHandlers.Add(chatId, handler);
                }
            }
        }

        public void UnsubscribeFromNewChatMessages(int chatId, EventHandler<IEnumerable<Message>> handler)
        {
            lock (_newChatMessagesHandlers)
            {
                if (!_newChatMessagesHandlers.ContainsKey(chatId))
                    throw new ArgumentException("No such subscription exists");
                _newChatMessagesHandlers[chatId] -= handler;
            }
        }

        public void SubscribeToNewUserInfo(int userId, EventHandler<User> handler)
        {
            lock (_newUserInfoHandlers)
            {
                if (_newUserInfoHandlers.ContainsKey(userId))
                {
                    _newUserInfoHandlers[userId] += handler;
                }
                else
                {
                    _newUserInfoHandlers.Add(userId, handler);
                }
            }
        }

        public void UnsubscribeFromNewUserInfo(int userId, EventHandler<User> handler)
        {
            lock (_newUserInfoHandlers)
            {
                if (!_newUserInfoHandlers.ContainsKey(userId))
                    throw new ArgumentException("No such userinfo exists");
                _newUserInfoHandlers[userId] -= handler;
            }
        }

        public void SubscribeToNewChatInfo(int chatId, EventHandler<Chat> handler)
        {
            lock (_newChatInfoHandlers)
            {
                if (_newChatInfoHandlers.ContainsKey(chatId))
                {
                    _newChatInfoHandlers[chatId] += handler;
                }
                else
                {
                    _newChatInfoHandlers.Add(chatId, handler);
                }
            }
        }

        public void UnsubscribeFromNewChatInfo(int chatId, EventHandler<Chat> handler)
        {
            lock (_newChatInfoHandlers)
            {
                if (!_newChatInfoHandlers.ContainsKey(chatId))
                    throw new ArgumentException("No such subscription exists");
                _newChatInfoHandlers[chatId] -= handler;
            }
        }

        public void SubscribeToNewChatUserInfo(int chatId, int userId, EventHandler<ChatUserInfo> handler)
        {
            var key = new ChatUserId(userId, chatId);
            lock (_newChatUserInfosHandlers)
            {
                if (_newChatUserInfosHandlers.ContainsKey(key))
                {
                    _newChatUserInfosHandlers[key] += handler;
                }
                else
                {
                    _newChatUserInfosHandlers.Add(key, handler);
                }
            }
        }

        public void UnsubscribeFromNewChatUserInfo(int chatId, int userId, EventHandler<ChatUserInfo> handler)
        {
            var key = new ChatUserId(userId, chatId);
            lock (_newChatUserInfosHandlers)
            {
                if (!_newChatUserInfosHandlers.ContainsKey(key))
                    throw new ArgumentException("No such subscription exists");
                _newChatUserInfosHandlers[key] -= handler;
            }
        }

        public void SubscribeToNewChatMembers(int chatId, EventHandler<IEnumerable<int>> handler)
        {
            lock (_newChatMembersHandlers)
            {
                if (_newChatMembersHandlers.ContainsKey(chatId))
                {
                    _newChatMembersHandlers[chatId] += handler;
                }
                else
                {
                    _newChatMembersHandlers.Add(chatId, handler);
                }
            }
        }

        public void UnsubscribeFromNewChatMembers(int chatId, EventHandler<IEnumerable<int>> handler)
        {
            lock (_newChatMembersHandlers)
            {
                if (!_newChatMembersHandlers.ContainsKey(chatId))
                    throw new ArgumentException("No such subscription exists");
                _newChatMembersHandlers[chatId] -= handler;
            }
        }

        public void SetUser(User user)
        {
            lock (UsersStorage)
            {
                if (UsersStorage.ContainsKey(user.Id))
                {
                    UsersStorage[user.Id] = user;
                    if (_newUserInfoHandlers.ContainsKey(user.Id))
                        _newUserInfoHandlers[user.Id]?.Invoke(this, user);
                }
                else
                {
                    UsersStorage.Add(user.Id, user);
                    NewUsersEvent?.Invoke(this, new[] {user});
                }
            }
        }

        public void SetUserInfo(int userId, UserInfo userInfo)
        {
            lock (UsersStorage)
            {
                if (!UsersStorage.ContainsKey(userId))
                    throw new ArgumentException("No such user exists");
                var user = UsersStorage[userId].CloneJson();
                user.UserInfo = userInfo;
                UsersStorage[userId] = user;
                if (_newUserInfoHandlers.ContainsKey(userId))
                {
                    _newUserInfoHandlers[userId]?.Invoke(this, UsersStorage[userId]);
                }
            }
        }

        public void SetChat(Chat chat)
        {
            lock (ChatsStorage)
            {
                if (ChatsStorage.ContainsKey(chat.Id))
                {
                    ChatsStorage[chat.Id] = chat;
                    if (_newChatInfoHandlers.ContainsKey(chat.Id))
                        _newChatInfoHandlers[chat.Id]?.Invoke(this, chat);
                }
                else
                {
                    ChatsStorage.Add(chat.Id, chat);
                    NewChatsEvent?.Invoke(this, new[] {chat});
                }
            }
        }

        public void SetChatUserInfo(int userId, int chatId, ChatUserInfo chatUserInfo)
        {
            lock (ChatUserInfosCache)
            {
                var key = new ChatUserId(userId, chatId);
                if (ChatUserInfosCache.ContainsKey(key))
                {
                    ChatUserInfosCache[key] = chatUserInfo;
                }
                else
                {
                    ChatUserInfosCache.Add(key, chatUserInfo);
                }
                if (_newChatUserInfosHandlers.ContainsKey(key))
                    _newChatUserInfosHandlers[key]?.Invoke(this, chatUserInfo);
            }
        }

        public void SendNewMessagesEvent(int chatId, IEnumerable<Message> newMessages)
        {
            lock (_newChatMessagesHandlers)
            {
                if (_newChatMessagesHandlers.ContainsKey(chatId))
                {
                    _newChatMessagesHandlers[chatId]?.Invoke(this, newMessages);
                }
            }
        }

        protected override HttpRequestMessage Request
        {
            get
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "events");
                var clientState = new ClientState
                {
                    LastChatId = _lastChatId,
                    LastUserId = _lastUserId
                };

                var usersStates = new List<UserInfoState>();
                lock (UsersStorage)
                {
                    usersStates.AddRange(UsersStorage.Values.Select(x => x)
                        .Select(user => new UserInfoState
                        {
                            UserId = user.Id,
                            UserHash = user.GetHashCode()
                        }));
                }
                clientState.UsersStates = usersStates;

                var chatsStates = new List<ChatInfoState>();
                lock (ChatsStorage)
                {
                    lock (ChatMessagesDictionary)
                        chatsStates.AddRange(ChatsStorage.Values.Select(chat => new ChatInfoState
                        {
                            ChatId = chat.Id,
                            ChatHash = chat.GetHashCode(),
                            LastChatMessageId = ChatMessagesDictionary.ContainsKey(chat.Id)
                                ? ChatMessagesDictionary[chat.Id].MessageId
                                : -1,
                            CurrentMembers = chat.Users.Select(x => new UserInfoState
                            {
                                UserId = x,
                                UserHash = ChatUserInfosCache.ContainsKey(new ChatUserId(x, chat.Id))
                                    ? ChatUserInfosCache[new ChatUserId(x, chat.Id)].GetHashCode()
                                    : -1
                            })
                        }));
                }
                clientState.ChatsStates = chatsStates;
                request.AddContent(clientState);
                return request;
            }
        }

        protected override async Task OnSuccessfulResponse(HttpResponseMessage response)
        {
            var resp = await response.Content.ReadAsAsync<ServerInfoOutput>();

            if (resp.NewChats != null && resp.NewChats.Any())
            {
                _lastChatId = resp.NewChats.Last().Id;
                lock (ChatsStorage)
                    foreach (var chat in resp.NewChats)
                        if (!ChatsStorage.ContainsKey(chat.Id))
                            ChatsStorage.Add(chat.Id, chat);
                NewChatsEvent?.Invoke(this, resp.NewChats);
                AreChatsPolled = true;
            }

            if (resp.NewUsers != null && resp.NewUsers.Any())
            {
                _lastUserId = resp.NewUsers.Last().Id;
                lock (UsersStorage)
                {
                    foreach (var user in resp.NewUsers)
                        if (!UsersStorage.ContainsKey(user.Id))
                            UsersStorage.Add(user.Id, user);
                }
                NewUsersEvent?.Invoke(this, resp.NewUsers);
            }

            foreach (var user in resp.UsersWithNewInfo)
            {
                lock (UsersStorage)
                    UsersStorage[user.Id] = user;

                lock (_newUserInfoHandlers)
                if (_newUserInfoHandlers.ContainsKey(user.Id))
                {
                    _newUserInfoHandlers[user.Id]?.Invoke(this, user);
                }
            }

            foreach (var chatInfo in resp.NewChatInfo)
            {
                if (chatInfo.Chat != null)
                {
                    lock (ChatsStorage)
                        ChatsStorage[chatInfo.ChatId] = chatInfo.Chat;
                    lock (_newUserInfoHandlers)
                    {
                        if (_newChatInfoHandlers.ContainsKey(chatInfo.ChatId))
                        {
                            _newChatInfoHandlers[chatInfo.ChatId]?.Invoke(this, chatInfo.Chat);
                        }
                    }
                }
                if (chatInfo.NewMembers != null && chatInfo.NewMembers.Any())
                {
                    lock (ChatsStorage)
                    {
                        ChatsStorage[chatInfo.ChatId].Users = ChatsStorage[chatInfo.ChatId].Users.Concat(chatInfo.NewMembers);
                    }
                    lock (_newChatMembersHandlers)
                    if (_newChatMembersHandlers.ContainsKey(chatInfo.ChatId))
                    {
                        _newChatMembersHandlers[chatInfo.ChatId]?.Invoke(this, chatInfo.NewMembers);
                    }
                }
                if (chatInfo.NewChatUserInfos != null && chatInfo.NewChatUserInfos.Any())
                {
                    lock (ChatUserInfosCache)
                    lock (_newChatUserInfosHandlers)
                        foreach (var userInfo in chatInfo.NewChatUserInfos)
                        {
                            var id = new ChatUserId(userInfo.Key, chatInfo.ChatId);
                            if (ChatUserInfosCache.ContainsKey(id))
                                ChatUserInfosCache[id] = userInfo.Value;
                            if (_newChatUserInfosHandlers.ContainsKey(id))
                            {
                                _newChatUserInfosHandlers[id]?.Invoke(this, userInfo.Value);
                            }
                        }
                }

                var wasPolled = true;
                lock (ChatMessagesDictionary)
                {
                    if (!ChatMessagesDictionary.ContainsKey(chatInfo.ChatId))
                    {
                        ChatMessagesDictionary.Add(chatInfo.ChatId, new MessageWithState {IsPolled = true, MessageId = -1});
                        wasPolled = false;
                    }
                    else if (!ChatMessagesDictionary[chatInfo.ChatId].IsPolled)
                    {
                        wasPolled = false;
                        ChatMessagesDictionary[chatInfo.ChatId].IsPolled = true;
                    }
                }
                if (chatInfo.NewChatMessages == null || !chatInfo.NewChatMessages.Any()) continue;
                    if (wasPolled)
                    NewMessagesEvent?.Invoke(this, chatInfo.NewChatMessages);

                lock (ChatMessagesDictionary)
                {
                    if (ChatMessagesDictionary.ContainsKey(chatInfo.ChatId))
                    {
                        ChatMessagesDictionary[chatInfo.ChatId].MessageId = chatInfo.NewChatMessages.Last().Id;
                    }
                    else
                    {
                        ChatMessagesDictionary.Add(chatInfo.ChatId, new MessageWithState{IsPolled = true, MessageId = chatInfo.NewChatMessages.Last().Id});
                    }
                }

                if (!wasPolled) continue;
                if (chatInfo.NewChatMessages.Any(x => x.SenderId != (_usersClient?.UserId ?? -1)))
                    lock (_newChatMessagesHandlers)
                    {
                        if (!_newChatMessagesHandlers.ContainsKey(chatInfo.ChatId)) continue;
                        _newChatMessagesHandlers[chatInfo.ChatId]?.Invoke(this,
                            chatInfo.NewChatMessages.Where(x => x.SenderId != (_usersClient?.UserId ?? -1)));
                    }
            }
        }
    }
}