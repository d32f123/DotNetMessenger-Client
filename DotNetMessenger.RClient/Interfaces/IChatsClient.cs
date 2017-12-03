using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.RClient.Interfaces
{
    public interface IChatsClient : IDisposable
    {
        Task<Chat> GetChatAsync(int chatId);
        Task<Chat> GetDialogChatAsync(int otherId);
        Task<ChatUserInfo> GetChatUserInfoAsync(int chatId);
        Task<ChatUserInfo> GetChatSpecificUserInfoAsync(int chatId, int userId);
        Task SetChatSpecificUserInfoAsync(int chatId, int userId, ChatUserInfo info);
        Task SetChatSpecificUserRoleAsync(int chatId, int userId, UserRoles role);
        Task<Chat> CreateNewGroupChat(string chatName, IEnumerable<int> users);
        Task<List<Chat>> GetUserChatsAsync();
        Task<bool> SetChatInfoAsync(int chatId, ChatInfo chatInfo);
        Task AddUsersAsync(int chatId, IEnumerable<int> userIds);
        Task KickUsersAsync(int chatId, IEnumerable<int> userIds);

        void SubscribeToNewChatInfo(int chatId, EventHandler<Chat> handler);
        void UnsubscribeFromNewChatInfo(int chatId, EventHandler<Chat> handler);
        void SubscribeToNewChatUserInfo(int chatId, int userId, EventHandler<ChatUserInfo> handler);
        void UnsubscribeFromNewChatUserInfo(int chatId, int userId, EventHandler<ChatUserInfo> handler);
        void SubscribeToNewChatMembers(int chatId, EventHandler<IEnumerable<int>> handler);
        void UnsubscribeFromNewChatMembers(int chatId, EventHandler<IEnumerable<int>> handler);

        event EventHandler<IEnumerable<Chat>> NewChatsEvent;
        event EventHandler<IEnumerable<int>> LostChatsEvent;
    }
}