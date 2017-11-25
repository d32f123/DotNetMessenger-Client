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
       
        event EventHandler<IEnumerable<Chat>> NewChatsEvent;
    }
}