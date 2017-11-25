using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetMessenger.RClient.LongPollers
{
    public abstract class LongPoller : IDisposable
    {
        private readonly HttpClient _client;
        protected abstract HttpRequestMessage Request { get; }

        private bool _threadStarted;
        private readonly Thread _pollerThread;
        private bool _threadShouldExit;
        private readonly object _lock = new object();
        private readonly int _intervalMilliseconds;

        

        protected abstract Task OnSuccessfulResponse(HttpResponseMessage response);

        protected LongPoller(string connectionString, Guid token, bool shouldStartImmediately = true, int intervalMilliseconds = 1000)
        {
            _client = new HttpClient { BaseAddress = new Uri(connectionString) };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{token}:".ToBase64String());

            _threadShouldExit = false;

            _intervalMilliseconds = intervalMilliseconds;
            _pollerThread = new Thread(PollerThread) {IsBackground = true};
            if (shouldStartImmediately)
            {
                Start();
            }
        }

        public void Start()
        {
            if (_threadStarted)
                throw new InvalidOperationException("LongPoller is already started!");
            _threadStarted = true;
            _pollerThread.Start();
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_threadStarted)
                    throw new InvalidOperationException("LongPoller was not even started!");
                _threadShouldExit = true;
            }
        }

        private async void PollerThread()
        {
            do
            {
                HttpResponseMessage response;
                try
                {
                    response = await _client.SendAsync(Request).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    response = null;
                }
                lock (_lock)
                {
                    if (_threadShouldExit)
                        break;
                }
                if (response != null && response.IsSuccessStatusCode)
                {
                    await OnSuccessfulResponse(response);
                }
                Thread.Sleep(_intervalMilliseconds);
            } while (true);
        }

        public void Dispose()
        {
            Stop();
            _pollerThread?.Join();

            _client?.Dispose();
        }
    }
}