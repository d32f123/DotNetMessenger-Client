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
        public event EventHandler<IEnumerable<int>> LostChatsEvent;
        public event EventHandler<IEnumerable<Message>> NewMessagesEvent;

        private readonly IUsersClient _usersClient;

        private readonly Dictionary<int, EventHandler<User>> _newUserInfoHandlers;
        private readonly Dictionary<int, EventHandler<Chat>> _newChatInfoHandlers;
        private readonly Dictionary<int, EventHandler<IEnumerable<Message>>> _newChatMessagesHandlers;
        private readonly Dictionary<int, EventHandler<IEnumerable<int>>> _newChatMembersHandlers;
        private readonly Dictionary<ChatUserId, EventHandler<ChatUserInfo>> _newChatUserInfosHandlers; 

        public NewEventsPoller(string connectionString, Guid token, IUsersClient usersClient, IChatsClient chatsClient) 
            : base(connectionString, token, true, 3000)
        {
            AreChatsPolled = false;
            UsersStorage = new Dictionary<int, User>();
            ChatsStorage = new Dictionary<int, Chat>();
            ChatUserInfosCache = new CacheStorage<ChatUserId, ChatUserInfo>();
            _lastUserId = -1;

            _newChatInfoHandlers = new Dictionary<int, EventHandler<Chat>>();
            _newChatMembersHandlers = new Dictionary<int, EventHandler<IEnumerable<int>>>();
            _newChatMessagesHandlers = new Dictionary<int, EventHandler<IEnumerable<Message>>>();
            _newChatUserInfosHandlers = new Dictionary<ChatUserId, EventHandler<ChatUserInfo>>();
            _newUserInfoHandlers = new Dictionary<int, EventHandler<User>>();

            usersClient.GetAllUsersAsync().Result.ForEach(x => UsersStorage.Add(x.Id, x));
            chatsClient.GetUserChatsAsync().Result.ForEach(x =>
            {
                if (!ChatsStorage.ContainsKey(x.Id))
                    ChatsStorage.Add(x.Id, x);
            });

            _usersClient = usersClient;
        }

        public bool AreChatsPolled { get; set; }

        public class MessageWithState
        {
            public int MessageId { get; set; }
            public bool IsPolled { get; set; }
        }

        private int _lastUserId;
        public readonly Dictionary<int, MessageWithState> ChatMessagesDictionary = new Dictionary<int, MessageWithState>();
        private IEnumerable<int> _polledChats;


        public Dictionary<int, User> UsersStorage { get; }
        public Dictionary<int, Chat> ChatsStorage { get; }
        public CacheStorage<ChatUserId, ChatUserInfo> ChatUserInfosCache { get; }

        #region Subscriptions

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

        #endregion

        #region Data setting
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
                    lock (_newChatInfoHandlers)
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

        public void SetChatInfo(int chatId, ChatInfo chatInfo)
        {
            lock (ChatsStorage)
            {
                if (!ChatsStorage.ContainsKey(chatId))
                {
                    throw new ArgumentException("Could not set chat info");
                }
                ChatsStorage[chatId] = ChatsStorage[chatId].CloneJson();
                ChatsStorage[chatId].Info = chatInfo;
                lock (_newChatInfoHandlers)
                    if (_newChatInfoHandlers.ContainsKey(chatId))
                        _newChatInfoHandlers[chatId]?.Invoke(this, ChatsStorage[chatId]);
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

        #endregion

        protected override HttpRequestMessage Request
        {
            get
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "events");
                var clientState = new ClientState
                {
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
                    _polledChats = new List<int>(ChatsStorage.Keys);
                    lock (ChatMessagesDictionary)
                    {
                        chatsStates.AddRange(ChatsStorage.Values.Select(chat => new ChatInfoState
                        {
                            ChatId = chat.Id,
                            ChatHash = chat.GetHashCode(),
                            LastChatMessageId = ChatMessagesDictionary.ContainsKey(chat.Id)
                                ? ChatMessagesDictionary[chat.Id].MessageId
                                : -1,
                            CurrentMembers = chat.Users?.Select(x => new UserInfoState
                            {
                                UserId = x,
                                UserHash = ChatUserInfosCache.ContainsKey(new ChatUserId(x, chat.Id))
                                    ? ChatUserInfosCache[new ChatUserId(x, chat.Id)].GetHashCode()
                                    : -1
                            })
                        }));
                    }
                }
                clientState.ChatsStates = chatsStates;
                request.AddContent(clientState);
                return request;
            }
        }

        private void ProcessNewChats(IEnumerable<Chat> chats)
        {
            if (chats == null) return;

            var chatsArray = chats as Chat[] ?? chats.ToArray();

            if (!chatsArray.Any()) return;

            lock (ChatsStorage)
                foreach (var chat in chatsArray)
                    if (!ChatsStorage.ContainsKey(chat.Id))
                        ChatsStorage.Add(chat.Id, chat);
            NewChatsEvent?.Invoke(this, chatsArray);
            AreChatsPolled = true;
        }

        private void ProcessLostChats(IEnumerable<int> chatIds)
        {
            if (chatIds == null) return;

            var chatIdsArray = chatIds as int[] ?? chatIds.ToArray();

            if (!chatIdsArray.Any()) return;

            lock (ChatsStorage)
                foreach (var chatId in chatIdsArray)
                    if (ChatsStorage.ContainsKey(chatId))
                        ChatsStorage.Remove(chatId);
            LostChatsEvent?.Invoke(this, chatIdsArray);
        }

        private void ProcessNewUsers(IEnumerable<User> users)
        {
            if (users == null) return;

            var usersArray = users as User[] ?? users.ToArray();

            if (!usersArray.Any()) return;

            _lastUserId = usersArray.Last().Id;
            lock (UsersStorage)
            {
                foreach (var user in usersArray)
                    if (!UsersStorage.ContainsKey(user.Id))
                        UsersStorage.Add(user.Id, user);
            }
            NewUsersEvent?.Invoke(this, usersArray);
        }

        private void ProcessUsersWithNewInfo(IEnumerable<User> users)
        {
            if (users == null) return;

            var usersArray = users as User[] ?? users.ToArray();

            if (!usersArray.Any()) return;

            foreach (var user in usersArray)
            {
                lock (UsersStorage)
                    UsersStorage[user.Id] = user;

                lock (_newUserInfoHandlers)
                    if (_newUserInfoHandlers.ContainsKey(user.Id))
                    {
                        _newUserInfoHandlers[user.Id]?.Invoke(this, user);
                    }
            }
        }

        private void ProcessNewChatDescription(Chat chat)
        {
            if (chat == null) return;

            lock (ChatsStorage)
                ChatsStorage[chat.Id] = chat;
            lock (_newUserInfoHandlers)
            {
                if (_newChatInfoHandlers.ContainsKey(chat.Id))
                {
                    _newChatInfoHandlers[chat.Id]?.Invoke(this, chat);
                }
            }
        }

        private void ProcessNewChatMembers(int chatId, IEnumerable<int> members)
        {
            if (members == null) return;

            var membersArray = members as int[] ?? members.ToArray();

            if (!membersArray.Any()) return;

            lock (ChatsStorage)
            {
                lock (ChatUserInfosCache)
                {
                    var prevMembers = ChatsStorage[chatId].Users;
                    foreach (var excludedMember in prevMembers.Where(x => !membersArray.Contains(x)))
                    {
                        var key = new ChatUserId(excludedMember, chatId);
                        if (ChatUserInfosCache.ContainsKey(key))
                            ChatUserInfosCache.Remove(key);
                    }
                }
                ChatsStorage[chatId].Users = membersArray;
            }

            lock (_newChatMembersHandlers)
            {
                if (_newChatMembersHandlers.ContainsKey(chatId))
                {
                    _newChatMembersHandlers[chatId]?.Invoke(this, membersArray);
                }
            }
        }

        private void ProcessNewChatUserInfos(int chatId, Dictionary<int, ChatUserInfo> chatUserInfos)
        {
            if (chatUserInfos == null || !chatUserInfos.Any()) return;

            lock (ChatUserInfosCache)
            {
                lock (_newChatUserInfosHandlers)
                {
                    foreach (var userInfo in chatUserInfos)
                    {
                        var id = new ChatUserId(userInfo.Key, chatId);
                        if (ChatUserInfosCache.ContainsKey(id))
                        {
                            ChatUserInfosCache[id] = userInfo.Value;
                        }
                        if (_newChatUserInfosHandlers.ContainsKey(id))
                        {
                            _newChatUserInfosHandlers[id]?.Invoke(this, userInfo.Value);
                        }
                    }
                }
            }
        }

        private void ProcessNewChatMessages(int chatId, IEnumerable<Message> messages)
        {
            if (messages == null) return;

            var messagesArray = messages as Message[] ?? messages.ToArray();

            if (!messagesArray.Any()) return;

            bool wasPolled;
            lock (ChatMessagesDictionary)
            {
                wasPolled = ChatMessagesDictionary.ContainsKey(chatId) && ChatMessagesDictionary[chatId].IsPolled;
            }

            if (wasPolled)
                NewMessagesEvent?.Invoke(this, messagesArray);

            lock (ChatMessagesDictionary)
            {
                if (ChatMessagesDictionary.ContainsKey(chatId))
                {
                    ChatMessagesDictionary[chatId].MessageId = messagesArray.Last().Id;
                }
                else
                {
                    ChatMessagesDictionary.Add(chatId, new MessageWithState { IsPolled = true, MessageId = messagesArray.Last().Id });
                }
            }

            if (!wasPolled) return;

            if (messagesArray.All(x => x.SenderId == (_usersClient?.UserId ?? -1))) return;

            lock (_newChatMessagesHandlers)
            {
                if (!_newChatMessagesHandlers.ContainsKey(chatId)) return;
                _newChatMessagesHandlers[chatId]?.Invoke(this,
                    messagesArray.Where(x => x.SenderId != (_usersClient?.UserId ?? -1)));
            }
        }

        private void ProcessNewChatInfos(IEnumerable<ChatInfoOutput> chatInfos)
        {
            foreach (var chatInfo in chatInfos)
            {
                ProcessNewChatDescription(chatInfo.Chat);

                ProcessNewChatMembers(chatInfo.ChatId, chatInfo.NewMembers);

                ProcessNewChatUserInfos(chatInfo.ChatId, chatInfo.NewChatUserInfos);

                ProcessNewChatMessages(chatInfo.ChatId, chatInfo.NewChatMessages);
            }
        }

        protected override async Task OnSuccessfulResponse(HttpResponseMessage response)
        {
            var resp = await response.Content.ReadAsAsync<ServerInfoOutput>();

            ProcessNewChats(resp.NewChats);

            ProcessLostChats(resp.LostChats);

            ProcessNewUsers(resp.NewUsers);

            ProcessUsersWithNewInfo(resp.UsersWithNewInfo);

            ProcessNewChatInfos(resp.NewChatInfo);

            // mark chats as polled
            foreach (var chatId in _polledChats)
            {
                lock (ChatMessagesDictionary)
                {
                    if (!ChatMessagesDictionary.ContainsKey(chatId))
                    {
                        ChatMessagesDictionary.Add(chatId, new MessageWithState { IsPolled = true, MessageId = -1 });
                    }
                    else
                    {
                        ChatMessagesDictionary[chatId].IsPolled = true;
                    }
                }
            }
        }
    }
}