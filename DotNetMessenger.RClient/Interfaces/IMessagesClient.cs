using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetMessenger.Model;

namespace DotNetMessenger.RClient.Interfaces
{
    public interface IMessagesClient : IDisposable
    {
        Task<Message> GetMessage(int id);
        Task<IEnumerable<Message>> GetChatMessagesAsync(int id);
        Task<IEnumerable<Message>> GetChatMessagesFromAsync(int chatId, DateTime dateFrom);
        Task<IEnumerable<Message>> GetChatMessagesToAsync(int chatId, DateTime dateTo);
        Task<IEnumerable<Message>> GetChatMessagesInRangeAsync(int chatId, DateTime? dateFrom, DateTime? dateTo);
        Task<IEnumerable<Message>> GetChatMessagesFromAsync(int chatId, int messageId);
        Task<Message> GetLatestChatMessageAsync(int id);
        Task SendMessageAsync(int chatId, Message message);

        void SubscribeToChat(int chatId, int lastMessageId, EventHandler<IEnumerable<Message>> newMessagesHandler);
        void UnsubscribeFromChat(int chatId, EventHandler<IEnumerable<Message>> handler);
    }
}