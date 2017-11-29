using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetMessenger.Model;

namespace DotNetMessenger.RClient.Interfaces
{
    public interface IChatsClient : IDisposable
    {
        Task<Chat> GetChatAsync(int chatId);
        Task<Chat> GetDialogChatAsync(int otherId);
        Task<ChatUserInfo> GetChatUserInfoAsync(int chatId);
        Task<ChatUserInfo> GetChatSpecificUserinfoAsync(int chatId, int userId);
        Task<Chat> CreateNewGroupChat(string chatName, IEnumerable<int> users);
        Task<List<Chat>> GetUserChatsAsync();

        void SubscribeToNewChatInfo(int chatId, EventHandler<Chat> handler);
        void UnsubscribeFromNewChatInfo(int chatId, EventHandler<Chat> handler);
        void SubscribeToNewChatUserInfo(int chatId, int userId, EventHandler<ChatUserInfo> handler);
        void UnsubscribeFromNewChatUserInfo(int chatId, int userId, EventHandler<ChatUserInfo> handler);
        void SubscribeToNewChatMembers(int chatId, EventHandler<IEnumerable<int>> handler);
        void UnsubscribeFromNewChatMembers(int chatId, EventHandler<IEnumerable<int>> handler);

        event EventHandler<IEnumerable<Chat>> NewChatsEvent;
    }
}