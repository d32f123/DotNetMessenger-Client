using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DotNetMessenger.Model;
using DotNetMessenger.RClient.Extensions;
using DotNetMessenger.RClient.Interfaces;
using DotNetMessenger.RClient.LongPollers;

namespace DotNetMessenger.RClient.Clients
{
    public class UsersClient : IUsersClient
    {
        private readonly HttpClient _client;
        private int _userId = -1;

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
                        _poller.NewUsersEvent -= PollerOnNewUsersEvent;

                    _poller = value;
                    _poller.NewUsersEvent += PollerOnNewUsersEvent;
                }
            }
        }

        private void PollerOnNewUsersEvent(object sender, IEnumerable<User> enumerable)
        {
            NewUsersEvent?.Invoke(this, enumerable);
        }

        public event EventHandler<IEnumerable<User>> NewUsersEvent;


        public UsersClient(string connectionString, Guid token)
        {
            var s = connectionString;
            var token1 = token;

            _client = new HttpClient { BaseAddress = new Uri(s) };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{token1}:".ToBase64String());
        }

        public int UserId
        {
            get
            {
                if (_userId != -1) return _userId;
                _userId = GetLoggedUserIdAsync().Result;
                if (_userId == -1)
                    throw new InvalidOperationException("Invalid token");
                return _userId;
            }
        }

        public async Task<int> GetLoggedUserIdAsync()
        {
            var response = await _client.GetAsync("tokens").ConfigureAwait(false);
            try
            {
                var retString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return response.IsSuccessStatusCode ? int.Parse(retString) : -1;
            }
            catch
            {
                return -1;
            }
        }

        public async Task<User> GetUserAsync(int id)
        {
            lock (_lock)
                if (_poller != null)
                    lock (_poller.UsersStorage)
                        if (_poller.UsersStorage.ContainsKey(id))
                            return _poller.UsersStorage[id];

            var response = await _client.GetAsync($"users/{id}").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) return null;

            var user = await response.Content.ReadAsAsync<User>().ConfigureAwait(false);
            return user;
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            lock (_lock)
                if (_poller != null)
                    lock (_poller.UsersStorage)
                    {
                        var ret = _poller.UsersStorage.Values.SingleOrDefault(x => x.Username == username);
                        if (ret != null) return ret;
                    }

            var response = await _client.GetAsync($"users/{username}").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) return null;

            var user = await response.Content.ReadAsAsync<User>().ConfigureAwait(false);
            return user;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            lock (_lock)
                if (_poller != null)
                    lock (_poller.UsersStorage)
                    {
                        if (_poller.UsersStorage.Count != 0)
                        {
                            var values = _poller.UsersStorage.Values.Select(x => x);
                            return values as List<User> ?? values.ToList();
                        }
                    }

            var response = await _client.GetAsync("users").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<List<User>>().ConfigureAwait(false);
            }
            return null;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            lock (_lock)
                if (_poller != null)
                    lock (_poller.UsersStorage)
                    {
                        if (_poller.UsersStorage.ContainsKey(id)) _poller.UsersStorage.Remove(id);
                    }
            return (await _client.DeleteAsync($"users/{id}").ConfigureAwait(false)).IsSuccessStatusCode;
        }

        public async Task<bool> SetUserInfoAsync(UserInfo userInfo)
        {
            var response = await _client.PutAsJsonAsync($"users/{UserId}/userinfo", userInfo);

            lock (_lock)
                if (_poller != null)
                    lock (_poller.UsersStorage)
                        if (response.IsSuccessStatusCode && _poller.UsersStorage.ContainsKey(UserId))
                            _poller.SetUserInfo(UserId, userInfo);

            return response.IsSuccessStatusCode;
        }

        public void SubscribeToNewUserInfo(int userId, EventHandler<User> handler)
        {
            lock (_lock)
                if (_poller == null)
                {
                    Trace.WriteLine("COULD NOT SUBSCRIBE BECUASE POLLER WAS NOT STARTED");
                }
                else
                {
                    _poller.SubscribeToNewUserInfo(userId, handler);
                }
        }

        public void UnsubscribeFromNewUserInfo(int userId, EventHandler<User> handler)
        {
            lock (_lock)
            {
                _poller?.UnsubscribeFromNewUserInfo(userId, handler);
            }
        }

        public void Dispose()
        {
        }
    }
}