namespace DotNetMessenger.RClient.Classes
{
    public class ChatUserId
    {
        public readonly int UserId;
        public readonly int ChatId;

        public ChatUserId(int userId, int chatId)
        {
            UserId = userId;
            ChatId = chatId;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ChatUserId other)) return false;
            return other.UserId == UserId && other.ChatId == ChatId;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (UserId * 397) ^ ChatId;
            }
        }
    }
}