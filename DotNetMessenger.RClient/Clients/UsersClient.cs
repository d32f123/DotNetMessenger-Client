using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DotNetMessenger.Model;
using DotNetMessenger.RClient.Interfaces;
using DotNetMessenger.RClient.LongPollers;
using DotNetMessenger.RClient.Storages;

namespace DotNetMessenger.RClient.Clients
{
    public class UsersClient : IUsersClient
    {
        private readonly HttpClient _client;
        private int _userId = -1;
        private readonly NewUserPoller _poller;

        public event EventHandler<IEnumerable<User>> NewUsersEvent;

        private readonly CacheStorage<int, User> _userCache = new CacheStorage<int, User>(100);
        private int _usersTotal;

        public UsersClient(string connectionString, Guid token)
        {
            var s = connectionString;
            var token1 = token;

            _client = new HttpClient { BaseAddress = new Uri(s) };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{token1}:".ToBase64String());

            _usersTotal = -1;
            var users = GetAllUsersAsync().Result;
            
            _userCache.AddRange(users.Select(x => new KeyValuePair<int, User>(x.Id, x)));
            _usersTotal = users.Count;

            NewUsersEvent += OnNewUsersEvent;

            _poller = new NewUserPoller(s, token1, -1, (_, e) => NewUsersEvent?.Invoke(this, e));
        }

        private void OnNewUsersEvent(object sender, IEnumerable<User> enumerable)
        {
            var users = enumerable as User[] ?? enumerable.ToArray();
            _usersTotal += users.Length;
            _userCache.AddRange(users.Select(x => new KeyValuePair<int, User>(x.Id, x)));
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
            if (_userCache.ContainsKey(id)) return _userCache[id];

            var response = await _client.GetAsync($"users/{id}").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) return null;

            var user = await response.Content.ReadAsAsync<User>().ConfigureAwait(false);
            if (user != null) _userCache.Add(id, user);
            return user;
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            var ret = _userCache.Values.SingleOrDefault(x => x.Username == username);
            if (ret != null) return ret;

            var response = await _client.GetAsync($"users/{username}").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) return null;

            var user = await response.Content.ReadAsAsync<User>().ConfigureAwait(false);
            if (user != null) _userCache.Add(user.Id, user);
            return user;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            if (_usersTotal == _userCache.Count) return _userCache.Values as List<User> ?? _userCache.Values.ToList();

            var response = await _client.GetAsync("users").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<List<User>>().ConfigureAwait(false);
            }
            return null;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            if (_userCache.ContainsKey(id)) _userCache.Remove(id);

            return (await _client.DeleteAsync($"users/{id}").ConfigureAwait(false)).IsSuccessStatusCode;
        }

        public async Task<bool> SetUserInfoAsync(UserInfo userInfo)
        {
            var response = await _client.PutAsJsonAsync($"users/{UserId}/userinfo", userInfo);

            if (response.IsSuccessStatusCode && _userCache.ContainsKey(UserId))
                _userCache[UserId].UserInfo = userInfo;

            return response.IsSuccessStatusCode;

        }

        public void Dispose()
        {
            _poller?.Dispose();
        }
    }
}