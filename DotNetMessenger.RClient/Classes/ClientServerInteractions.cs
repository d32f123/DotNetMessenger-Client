using System.Collections.Generic;
using DotNetMessenger.Model;

namespace DotNetMessenger.RClient.Classes
{
    public class ChatInfoState
    {
        public int ChatId { get; set; }

        // Chat ^ [ChatInfo]
        public int ChatHash { get; set; }

        // user id + user hash
        public IEnumerable<UserInfoState> CurrentMembers { get; set; }

        public int LastChatMessageId { get; set; }
    }

    public class UserInfoState
    {
        public int UserId { get; set; }

        public int UserHash { get; set; }
    }

    public class ClientState
    {
        public int LastUserId { get; set; }

        public IEnumerable<UserInfoState> UsersStates { get; set; }

        public IEnumerable<ChatInfoState> ChatsStates { get; set; }
    }

    public class ChatInfoOutput
    {
        public int ChatId { get; set; }

        public Chat Chat { get; set; }

        public IEnumerable<int> NewMembers { get; set; }

        public Dictionary<int, ChatUserInfo> NewChatUserInfos { get; set; }

        public IEnumerable<Message> NewChatMessages { get; set; }
    }

    public class ServerInfoOutput
    {
        public IEnumerable<User> NewUsers { get; set; }

        public IEnumerable<Chat> NewChats { get; set; }

        public IEnumerable<int> LostChats { get; set; }

        public IEnumerable<User> UsersWithNewInfo { get; set; }

        public IEnumerable<ChatInfoOutput> NewChatInfo { get; set; }
    }
}