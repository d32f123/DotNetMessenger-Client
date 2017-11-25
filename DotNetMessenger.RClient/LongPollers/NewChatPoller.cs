using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetMessenger.Model;

namespace DotNetMessenger.RClient.LongPollers
{
    public class NewChatPoller : LongPoller
    {
        public event EventHandler<IEnumerable<Chat>> NewChatsEvent;

        private int _lastEntityReceived;

        public NewChatPoller(string connectionString, Guid token, int lastEntityReceived, 
            EventHandler<IEnumerable<Chat>> handler, bool shouldStart = true) 
            : base(connectionString, token, shouldStart, 2000)
        {
            NewChatsEvent += handler;
            _lastEntityReceived = lastEntityReceived;
        }

        private bool _isFirstPool = true;

        protected override HttpRequestMessage Request => new HttpRequestMessage(HttpMethod.Get, $"chats/subscribe/{_lastEntityReceived}");

        protected override async Task OnSuccessfulResponse(HttpResponseMessage response)
        {
            if (_isFirstPool)
            {
                _lastEntityReceived = await GetLastEntity(response);
                _isFirstPool = false;
                return;
            } 
            var list = await response.Content.ReadAsAsync<List<Chat>>();
            if (list == null || !list.Any()) return;
            _lastEntityReceived = list.Last().Id;
            NewChatsEvent?.Invoke(this, list);
        }

        private async Task<int> GetLastEntity(HttpResponseMessage response)
        {
            var list = await response.Content.ReadAsAsync<List<Chat>>();
            if (list == null || !list.Any()) return -1;
            return list.Last().Id;
        }
    }
}