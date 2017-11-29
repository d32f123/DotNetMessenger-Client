using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using DotNetMessenger.Model;
using DotNetMessenger.RClient.Extensions;
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
                        _poller.NewMessagesEvent -= NewMessagesHandler;

                    _poller = value;
                    _poller.NewMessagesEvent += NewMessagesHandler;
                }
            }
        }

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly CacheStorage<int, List<Message>> _messageCache = new CacheStorage<int, List<Message>>(10);

        public MessagesClient(string connectionString, Guid token, IUsersClient usersClient)
        {
            _usersClient = usersClient;

            _client = new HttpClient { BaseAddress = new Uri(connectionString) };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{token}:".ToBase64String());

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
            try
            {
                if (!_messageCache.ContainsKey(chatId))
                {
                    _semaphore.Release();
                    // this call should put the messages in cache
                    await GetChatMessagesAsync(chatId);
                    await _semaphore.WaitAsync();
                }
                // if for some reason we still don't have the messages in cache, just ask the server
                if (!_messageCache.ContainsKey(chatId))
                {
                    _semaphore.Release();

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
            try
            {
                if (!_messageCache.ContainsKey(chatId))
                {
                    _semaphore.Release();
                    // this call should put the messages in cache
                    await GetChatMessagesAsync(chatId);
                    await _semaphore.WaitAsync();
                }
                // if for some reason we still don't have the messages in cache, just ask the server
                if (!_messageCache.ContainsKey(chatId))
                {
                    _semaphore.Release();
                    var response = await _client.GetAsync($"messages/chats/{chatId}/from/{messageId}")
                        .ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode) return null;

                    return await response.Content.ReadAsAsync<List<Message>>();
                }
                _messageCache[chatId].RemoveAll(x => x.ExpirationDate != null && x.ExpirationDate < DateTime.Now);
                var cached = _messageCache[chatId].Where(x => x.Id > messageId);
                _semaphore.Release();
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
            try
            {
                if (_messageCache.ContainsKey(id))
                {
                    _messageCache[id].RemoveAll(x => x.ExpirationDate != null && x.ExpirationDate < DateTime.Now);
                    var cached = _messageCache[id].LastOrDefault();
                    _semaphore.Release();
                    return cached;
                }
                _semaphore.Release();

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
            await _semaphore.WaitAsync();

            Task.Run(() => _poller.SendNewMessagesEvent(chatId, new[] {message}));

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
            OnNewMessagesParseStart?.Invoke(this, EventArgs.Empty);
            lock (_messageCache)
            {
                var messages = enumerable as List<Message> ?? enumerable.ToList();
                var chatId = messages.First().ChatId;
                if (!_messageCache.ContainsKey(chatId))
                {
                    _messageCache.Add(chatId, messages);
                }
                else
                {
                    _messageCache[chatId].AddRange(messages);
                }
            }
        }

        public void SubscribeToChat(int chatId, int lastMessageId, EventHandler<IEnumerable<Message>> newMessagesHandler)
        {
            Task.Run(async () =>
            {
                var messages = (await GetChatMessagesFromAsync(chatId, lastMessageId)).ToArray();
                if (messages.Any())
                    newMessagesHandler?.Invoke(this, messages);
            });


            _poller.SubscribeToNewChatMessages(chatId, newMessagesHandler);
        }

        public void UnsubscribeFromChat(int chatId, EventHandler<IEnumerable<Message>> handler)
        {
                _poller.UnsubscribeFromNewChatMessages(chatId, handler);
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
        }
    }
}