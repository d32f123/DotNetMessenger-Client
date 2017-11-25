using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using DotNetMessenger.Model;
using DotNetMessenger.RClient.Interfaces;
using DotNetMessenger.RClient.LongPollers;
using DotNetMessenger.RClient.Storages;

namespace DotNetMessenger.RClient.Clients
{
    public class MessagesClient : IMessagesClient
    {
        private readonly HttpClient _client;
        private readonly IUsersClient _usersClient;
        private int UserId => _usersClient.UserId;

        private readonly NewMessagePoller _messagePoller;
        private readonly Dictionary<int, EventHandler<IEnumerable<Message>>> _newChatMessageEvents =
            new Dictionary<int, EventHandler<IEnumerable<Message>>>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _newMessagesSemaphore = new SemaphoreSlim(1, 1);
        private readonly CacheStorage<int, List<Message>> _messageCache = new CacheStorage<int, List<Message>>(10);

        public MessagesClient(string connectionString, Guid token, IUsersClient usersClient)
        {
            var token1 = token;
            _usersClient = usersClient;

            _client = new HttpClient { BaseAddress = new Uri(connectionString) };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{token1}:".ToBase64String());

            _messagePoller = new NewMessagePoller(connectionString, token1, null, NewMessagesHandler);
        }

        public async Task<Message> GetMessage(int id)
        {
            var response = await _client.GetAsync($"messages/{id}").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<Message>();
            }
            return null;
        }

        public async Task<IEnumerable<Message>> GetChatMessagesAsync(int id)
        {
            await _semaphore.WaitAsync();
            await _newMessagesSemaphore.WaitAsync();
            try
            {
                if (_messageCache.ContainsKey(id))
                {
                    _messageCache[id].RemoveAll(x => x.ExpirationDate != null && x.ExpirationDate < DateTime.Now);
                    return _messageCache[id];
                }

                var response = await _client.GetAsync($"messages/chats/{id}").ConfigureAwait(false);

                if (!response.IsSuccessStatusCode) return null;

                var ret = await response.Content.ReadAsAsync<List<Message>>();

                _messageCache.Add(id, new List<Message>(ret));
                return ret;
            }
            catch (Exception e)
            {
                Trace.Write(e.ToString());
                throw;
            }
            finally
            {
                _semaphore.Release();
                _newMessagesSemaphore.Release();
            }
        }

        public async Task<IEnumerable<Message>> GetChatMessagesFromAsync(int chatId, DateTime dateFrom)
        {
            return await GetChatMessagesInRangeAsync(chatId, dateFrom, null);
        }

        public async Task<IEnumerable<Message>> GetChatMessagesToAsync(int chatId, DateTime dateTo)
        {
            return await GetChatMessagesInRangeAsync(chatId, null, dateTo);
        }

        public async Task<IEnumerable<Message>> GetChatMessagesInRangeAsync(int chatId, DateTime? dateFrom,
            DateTime? dateTo)
        {
            await _semaphore.WaitAsync();
            await _newMessagesSemaphore.WaitAsync();
            try
            {
                if (!_messageCache.ContainsKey(chatId))
                {
                    _semaphore.Release();
                    _newMessagesSemaphore.Release();
                    // this call should put the messages in cache
                    await GetChatMessagesAsync(chatId);
                    await _semaphore.WaitAsync();
                    await _newMessagesSemaphore.WaitAsync();
                }
                // if for some reason we still don't have the messages in cache, just ask the server
                if (!_messageCache.ContainsKey(chatId))
                {
                    _semaphore.Release();
                    _newMessagesSemaphore.Release();

                    var response = await _client
                        .PutAsJsonAsync($"messages/chats/{chatId}/bydate",
                            new DateRange { DateFrom = dateFrom, DateTo = dateTo })
                        .ConfigureAwait(false);


                    if (!response.IsSuccessStatusCode) return null;

                    return await response.Content.ReadAsAsync<List<Message>>();
                }
                // otherwise just return the messages from cache
                _messageCache[chatId].RemoveAll(x => x.ExpirationDate != null && x.ExpirationDate < DateTime.Now);
                var cached = _messageCache[chatId].Where(x =>
                    x.Date >= (dateFrom ?? DateTime.MinValue) && x.Date <= (dateTo ?? DateTime.MaxValue));
                _semaphore.Release();
                _newMessagesSemaphore.Release();
                return cached;
            }
            catch (Exception e)
            {
                Trace.Write(e.ToString());
                throw;
            }
        }

        public async Task<IEnumerable<Message>> GetChatMessagesFromAsync(int chatId, int messageId)
        {
            await _semaphore.WaitAsync();
            await _newMessagesSemaphore.WaitAsync();
            try
            {
                if (!_messageCache.ContainsKey(chatId))
                {
                    _semaphore.Release();
                    _newMessagesSemaphore.Release();
                    // this call should put the messages in cache
                    await GetChatMessagesAsync(chatId);
                    await _semaphore.WaitAsync();
                    await _newMessagesSemaphore.WaitAsync();
                }
                // if for some reason we still don't have the messages in cache, just ask the server
                if (!_messageCache.ContainsKey(chatId))
                {
                    _semaphore.Release();
                    _newMessagesSemaphore.Release();
                    var response = await _client.GetAsync($"messages/chats/{chatId}/from/{messageId}")
                        .ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode) return null;

                    return await response.Content.ReadAsAsync<List<Message>>();
                }
                _messageCache[chatId].RemoveAll(x => x.ExpirationDate != null && x.ExpirationDate < DateTime.Now);
                var cached = _messageCache[chatId].Where(x => x.Id > messageId);
                _semaphore.Release();
                _newMessagesSemaphore.Release();
                return cached;  
            }
            catch (Exception e)
            {
                Trace.Write(e.ToString());
                throw;
            }
        }

        public async Task<Message> GetLatestChatMessageAsync(int id)
        {
            await _semaphore.WaitAsync();
            await _newMessagesSemaphore.WaitAsync();
            try
            {
                if (_messageCache.ContainsKey(id))
                {
                    _messageCache[id].RemoveAll(x => x.ExpirationDate != null && x.ExpirationDate < DateTime.Now);
                    var cached = _messageCache[id].Last();
                    _semaphore.Release();
                    _newMessagesSemaphore.Release();
                    return cached;
                }
                _semaphore.Release();
                _newMessagesSemaphore.Release();

                var response = await _client.GetAsync($"messages/{id}/last").ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return null;

                return await response.Content.ReadAsAsync<Message>();
            }
            catch (Exception e)
            {
                Trace.Write(e.ToString());
                throw;
            }
        }

        public async Task SendMessageAsync(int chatId, Message message)
        {
            lock (_newChatMessageEvents)
            {
                if (_newChatMessageEvents.ContainsKey(chatId))
                {
                    _newChatMessageEvents[chatId]?.Invoke(this, new[] { message });
                }
            }
            await _semaphore.WaitAsync();
            var response = await _client.PostAsJsonAsync($"messages/{chatId}/{UserId}", message).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                OnNewMessagesParseStart += ParseHandler;
            }
            else
            {
                _semaphore.Release();
            }
        }

        private void ParseHandler(object o, EventArgs a)
        {
            _semaphore.Release();
            OnNewMessagesParseStart -= ParseHandler;
        }
        private event EventHandler OnNewMessagesParseStart;
        
        private void NewMessagesHandler(object sender, IEnumerable<Message> enumerable)
        {
            var toBeInvoked = new List<Message>();

            var lastChatId = -1;
            _newMessagesSemaphore.Wait();
            OnNewMessagesParseStart?.Invoke(this, EventArgs.Empty);
            foreach (var msg in enumerable)
            {
                if (lastChatId != msg.Id)
                {
                    lock (_newChatMessageEvents)
                    {
                        if (lastChatId != -1 && _messageCache.ContainsKey(lastChatId))
                            _messageCache[lastChatId].AddRange(toBeInvoked);
                        if (lastChatId != -1 && _newChatMessageEvents.ContainsKey(lastChatId))
                        {
                            if (toBeInvoked.Any(x => x.SenderId != UserId))
                                _newChatMessageEvents[lastChatId]?.Invoke(this, toBeInvoked.Where(x => x.SenderId != UserId));
                        }
                    }
                    lastChatId = msg.ChatId;
                    toBeInvoked = new List<Message>();
                }
                toBeInvoked.Add(msg);
            }
            lock (_newChatMessageEvents)
            {
                if (lastChatId != -1 && _messageCache.ContainsKey(lastChatId))
                    _messageCache[lastChatId].AddRange(toBeInvoked);
                if (lastChatId != -1 && _newChatMessageEvents.ContainsKey(lastChatId))
                {
                    if (toBeInvoked.Any(x => x.SenderId != UserId))
                        _newChatMessageEvents[lastChatId]?.Invoke(this, toBeInvoked.Where(x => x.SenderId != UserId));
                }
            }
            _newMessagesSemaphore.Release();
        }

        public void SubscribeToChat(int chatId, int lastMessageId, EventHandler<IEnumerable<Message>> newMessagesHandler)
        {
            Task.Run(async () =>
            {
                var messages = (await GetChatMessagesFromAsync(chatId, lastMessageId)).ToArray();
                if (messages.Any())
                    newMessagesHandler?.Invoke(this, messages);
            });

            lock (_newChatMessageEvents)
            {
                // if no subscription yet, create it
                if (!_newChatMessageEvents.ContainsKey(chatId))
                {
                    _newChatMessageEvents.Add(chatId, newMessagesHandler);
                    _messagePoller.AddChat(chatId);
                }
                else
                {
                    _newChatMessageEvents[chatId] += newMessagesHandler;
                }
            }
        }

        public void UnsubscribeFromChat(int chatId, EventHandler<IEnumerable<Message>> handler)
        {
            lock (_newChatMessageEvents)
            {
                if (!_newChatMessageEvents.ContainsKey(chatId))
                    throw new ArgumentException("No such subscription!");
                _newChatMessageEvents[chatId] -= handler;
                if (_newChatMessageEvents[chatId] != null && _newChatMessageEvents[chatId].GetInvocationList().Any()) return;
                _messagePoller.RemoveChat(chatId);
                _newChatMessageEvents.Remove(chatId);
            }
        }

        private class DateRange
        {
            public DateTime? DateFrom { get; set; }
            public DateTime? DateTo { get; set; }
        }

        public void Dispose()
        {
            _client?.Dispose();
            _usersClient?.Dispose();
            _messagePoller?.Dispose();
        }
    }
}