using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetMessenger.Model;

namespace DotNetMessenger.RClient.Interfaces
{
    public interface IUsersClient : IDisposable
    {
        int UserId { get; }

        Task<int> GetLoggedUserIdAsync();
        Task<User> GetUserAsync(int id);
        Task<User> GetUserByUsernameAsync(string username);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> DeleteUserAsync(int id);
        Task<bool> SetUserInfoAsync(UserInfo userInfo);

        event EventHandler<IEnumerable<User>> NewUsersEvent;
    }
}
