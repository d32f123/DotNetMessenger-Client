using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace DotNetMessenger.WPFClient.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            VerifyPropertyName(propertyName);
            var handler = PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real, 
            // public, instance property on this object. 
            if (TypeDescriptor.GetProperties(this)[propertyName] != null) return;
            var msg = "Invalid property name: " + propertyName;
            if (ThrowOnInvalidPropertyName)
                throw new Exception(msg);
            Debug.Fail(msg);
        }

        protected virtual bool ThrowOnInvalidPropertyName { get; } =
#if DEBUG
            true;
#else
            false;
#endif

        public event EventHandler CloseRequested;
        private ICommand _closeCommand;
        public ICommand CloseCommand
        {
            get
            {
                if (_closeCommand == null)
                {
                    _closeCommand = new RelayCommand(x => CloseRequested?.Invoke(this, EventArgs.Empty));
                }
                return _closeCommand;
            }
        }
    }
}