using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetMessenger.Model;

namespace DotNetMessenger.RClient.LongPollers
{
    public class NewMessagePoller : LongPoller
    {
        public event EventHandler<IEnumerable<Message>> NewMessagesEvent;

        private class ChatStatus
        {
            public bool IsPolled;
            public int LastMessage;

            public ChatStatus(bool isPolled, int lastMessage)
            {
                IsPolled = isPolled;
                LastMessage = lastMessage;
            }
        }
        // This messages will be sent to server (contain only last message id and chat id)
        private readonly Dictionary<int, ChatStatus> _chatMessagesDictionary = new Dictionary<int, ChatStatus>();

        public NewMessagePoller(string connectionString, Guid token, IEnumerable<int> chats,
            EventHandler<IEnumerable<Message>> handler, bool shouldStart = true)
            : base(connectionString, token, shouldStart)
        {
            if (chats != null)
            {
                var chatArray = chats as int[] ?? chats.ToArray();
                if (chatArray.Any())
                    foreach (var chat in chatArray)
                    {
                        _chatMessagesDictionary.Add(chat, new ChatStatus(false, -1));
                    }
            }
            NewMessagesEvent += handler;
        }
        
        public void AddChat(int chatId)
        {
            lock (_chatMessagesDictionary)
            {
                if (_chatMessagesDictionary.ContainsKey(chatId))
                    throw new ArgumentException("AddChat failed: already subscribed");
                _chatMessagesDictionary.Add(chatId, new ChatStatus(false, -1));
            }
        }

        public void RemoveChat(int chatId)
        {
            lock (_chatMessagesDictionary)
            {
                if (!_chatMessagesDictionary.ContainsKey(chatId))
                    throw new ArgumentException("RemoveChat failed: not even subscribed");
                _chatMessagesDictionary.Remove(chatId);
            }
        }

        protected override HttpRequestMessage Request
        {
            get
            {
                var request = new HttpRequestMessage(HttpMethod.Put, "chats/messages/subscribe");
                lock (_chatMessagesDictionary)
                {
                    request.AddContent(_chatMessagesDictionary.Select(x => new Message {Id = x.Value.LastMessage, ChatId = x.Key}));
                }
                return request;
            }
        }

        protected override async Task OnSuccessfulResponse(HttpResponseMessage response)
        {
            var list = await response.Content.ReadAsAsync<List<Message>>();
            if (list == null) return;

            var lastMessageId = -2;
            var lastChatId = -1;
            var msgsToBeInvoked = new List<Message>(list.Count);
            var shouldBeInvoked = false;
            var shouldBeSkipped = false;
            foreach (var msg in list)
            {
                if (msg.ChatId != lastChatId)
                {
                    lock (_chatMessagesDictionary)
                    {
                        if (!shouldBeSkipped && lastChatId != -1 )
                        {
                            _chatMessagesDictionary[lastChatId].LastMessage = lastMessageId;
                            _chatMessagesDictionary[lastChatId].IsPolled = true;
                        }
                        lastChatId = msg.ChatId;
                        lastMessageId = msg.Id;
                        shouldBeSkipped = !_chatMessagesDictionary.ContainsKey(msg.ChatId);
                        if (shouldBeSkipped)
                            continue;
                        shouldBeInvoked = _chatMessagesDictionary[lastChatId].IsPolled;
                    }
                }
                lastMessageId = msg.Id;
                if (shouldBeSkipped)
                    continue;
                if (shouldBeInvoked)
                    msgsToBeInvoked.Add(msg);
            }
            msgsToBeInvoked.TrimExcess();

            lock (_chatMessagesDictionary)
            {
                if (!shouldBeSkipped && lastChatId != -1)
                {
                    _chatMessagesDictionary[lastChatId].LastMessage = lastMessageId;
                    _chatMessagesDictionary[lastChatId].IsPolled = true;
                }
            }

            lock (_chatMessagesDictionary)
            foreach (var status in _chatMessagesDictionary.Values.Where(x => !x.IsPolled))
            {
                status.IsPolled = true;
            }

            if (msgsToBeInvoked.Any())
                NewMessagesEvent?.Invoke(this, msgsToBeInvoked);
        }
    }
}
