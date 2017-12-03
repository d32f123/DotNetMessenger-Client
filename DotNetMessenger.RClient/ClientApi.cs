using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DotNetMessenger.Model;
using DotNetMessenger.RClient.Clients;
using DotNetMessenger.RClient.Extensions;
using DotNetMessenger.RClient.Interfaces;
using DotNetMessenger.RClient.LongPollers;

namespace DotNetMessenger.RClient
{
    public static class ClientApi
    {
        private const string ConnectionString = @"http://94.19.250.182:58302/api/";
        private static readonly HttpClient Client;
        private static Guid _token = Guid.Empty;

        private static NewEventsPoller _poller;

        private static readonly object UsersLock = new object();
        private static bool _usersCreated;
        private static UsersClient _usersClient;
        public static IUsersClient UsersClient
        {
            get
            {
                if (_token == Guid.Empty)
                    throw new InvalidOperationException("Log in first");
                lock (UsersLock)
                {
                    if (_usersCreated) return _usersClient;
                    _usersCreated = true;
                    return _usersClient = new UsersClient(ConnectionString, _token);
                }
            }
        }

        private static readonly object ChatsLock = new object();
        private static bool _chatsCreated;
        private static ChatsClient _chatsClient;
        public static IChatsClient ChatsClient
        {
            get
            {
                if (_token == Guid.Empty)
                    throw new InvalidOperationException("Log in first");
                lock (ChatsLock)
                {
                    if (_chatsCreated) return _chatsClient;
                    _chatsCreated = true;
                    return _chatsClient = new ChatsClient(ConnectionString, _token, UsersClient);
                }
            }
        }

        private static readonly object MessagesLock = new object();
        private static bool _messagesCreated;
        private static MessagesClient _messagesClient;
        public static IMessagesClient MessagesClient
        {
            get
            {
                if (_token == Guid.Empty)
                    throw new InvalidOperationException("Log in first");
                lock (MessagesLock)
                {
                    if (_messagesCreated) return _messagesClient;
                    _messagesCreated = true;
                    return _messagesClient = new MessagesClient(ConnectionString, _token, UsersClient);
                }
            }
        }

        public static int UserId => _token == Guid.Empty
            ? throw new InvalidOperationException("Log in first")
            : UsersClient.UserId;

        public class UserCredentials
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        static ClientApi()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Client = new HttpClient { BaseAddress = new Uri(ConnectionString) };
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            Trace.Write(unhandledExceptionEventArgs.ToString());
        }

        public static async Task<User> RegisterAsync(string username, string password)
        {
            var response = await Client.PostAsJsonAsync("users", new UserCredentials { Username = username, Password = password });
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsAsync<User>();
            return null;
        }

        public static async Task<bool> LoginAsync(string login, string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "tokens");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", $"{login}:{password}".ToBase64String());
            try
            {
                var response = await Client.SendAsync(request).ConfigureAwait(false);
                var retString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    _token = Guid.Parse(new string(retString.Where(c => c != '"').ToArray()));



                    _poller = new NewEventsPoller(ConnectionString, _token, UsersClient, ChatsClient);
                    _usersClient.Poller = _poller;
                    _chatsClient.Poller = _poller;
                    ((MessagesClient) MessagesClient).Poller = _poller;
                    return true;
                }
                _token = Guid.Empty;
                return false;
            }
            catch (Exception)
            {
                _token = Guid.Empty;
                return false;
            }
        }

        public static async Task LogOut()
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "tokens");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{_token}:".ToBase64String());
            await Client.SendAsync(request).ConfigureAwait(false);
            _token = Guid.Empty;

            lock (UsersLock)
            {
                _usersClient.Dispose();
                _usersClient = null;
                _usersCreated = false;
            }

            lock (ChatsLock)
            {
                _chatsClient.Dispose();
                _chatsClient = null;
                _chatsCreated = false;
            }

            lock (MessagesLock)
            {
                _messagesClient?.Dispose();
                _messagesClient = null;
                _messagesCreated = false;
            }

            _poller?.Dispose();
            _poller = null;
        }
    }
}
