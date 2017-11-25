using System;

namespace DotNetMessenger.WPFClient.ViewModels.Entities
{
    public abstract class EntityViewModel : ViewModelBase
    {
        public abstract string MainString { get; }
        public abstract string SecondaryString { get; }
        public abstract byte[] Image { get; }
        public abstract DateTime Date { get; }
    }
}