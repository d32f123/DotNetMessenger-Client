namespace DotNetMessenger.WPFClient.Validation
{
    public class ValidationCustomError
    {
        public ValidationCustomError(string propName, string errorId, string errorMessage)
        {
            PropertyName = propName;
            Id = errorId;
            Msg = errorMessage;
        }

        public override string ToString() { return Msg; }

        public string Description => Msg;

        public readonly string Id;
        public readonly string Msg;
        public readonly string PropertyName;

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is ValidationCustomError other)) return false;
            return Equals(other);
        }

        public bool Equals(ValidationCustomError other)
        {
            if (other == null)
                return false;
            return (PropertyName == other.PropertyName && Id == other.Id);
        }

        public override int GetHashCode()
        {
            return PropertyName.GetHashCode() ^ Id.GetHashCode();
        }
    }
}