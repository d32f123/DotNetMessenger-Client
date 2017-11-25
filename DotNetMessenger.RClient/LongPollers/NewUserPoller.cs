using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetMessenger.Model;

namespace DotNetMessenger.RClient.LongPollers
{
    public class NewUserPoller : LongPoller
    {
        public event EventHandler<IEnumerable<User>> NewUsersEvent;

        private int _lastEntityReceived;

        public NewUserPoller(string connectionString, Guid token, int lastUserReceived, 
            EventHandler<IEnumerable<User>> handler, bool shouldStart = true)
            : base(connectionString, token, shouldStart, 2000)
        {
            NewUsersEvent += handler;
            _lastEntityReceived = lastUserReceived;
        }

        protected override HttpRequestMessage Request => new HttpRequestMessage(HttpMethod.Get, $"users/subscribe/{_lastEntityReceived}");

        protected override async Task OnSuccessfulResponse(HttpResponseMessage response)
        {
            if (_lastEntityReceived == -1)
            {
                _lastEntityReceived = await GetLastEntity(response);
                return;
            }
            var list = await response.Content.ReadAsAsync<List<User>>();
            if (list == null || !list.Any()) return;
            _lastEntityReceived = list.Last().Id;
            NewUsersEvent?.Invoke(this, list);
        }

        private async Task<int> GetLastEntity(HttpResponseMessage response)
        {
            var list = await response.Content.ReadAsAsync<List<User>>();
            if (list == null || !list.Any()) return -1;
            return list.Last().Id;
        }
    }
}