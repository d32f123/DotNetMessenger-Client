using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DotNetMessenger.WPFClient.ViewModels.Entities
{
    public abstract class EntityViewModel : ViewModelBase
    {
        public abstract string MainString { get; }
        public abstract string SecondaryString { get; }
        public abstract byte[] Image { get; }
        public abstract DateTime Date { get; }

        public abstract ObservableCollection<ContextAction> ContextActions { get; set; }
    }
}