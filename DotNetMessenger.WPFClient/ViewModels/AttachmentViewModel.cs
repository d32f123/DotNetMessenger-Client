using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.WPFClient.ViewModels
{
    public class AttachmentViewModel : ViewModelBase
    {
        private Attachment _attachment;
        public Attachment Attachment
        {
            get => _attachment;
            set
            {
                if (_attachment == value) return;
                _attachment = value;
                OnPropertyChanged(nameof(Attachment));
                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Content));
            }
        }

        public byte[] Icon => _attachment?.Type == AttachmentTypes.Image ? _attachment.File : null;
        public string Name => _attachment?.FileName;
        public byte[] Content => _attachment?.File;

        private ICommand _showFileCommand;
        private ICommand _saveFileCommand;

        public ICommand ShowFileCommand
        {
            get
            {
                if (_showFileCommand == null)
                {
                    _showFileCommand = new RelayCommand(x => ShowFile(), x => _attachment?.Type == AttachmentTypes.Image);
                }
                return _showFileCommand;
            }
        }

        public ICommand SaveFileCommand
        {
            get
            {
                if (_saveFileCommand == null)
                {
                    _saveFileCommand = new RelayCommand(x => SaveFile(), x => _attachment?.File != null);
                }
                return _saveFileCommand;
            }
        }

        public void ShowFile()
        {
            var filename = Path.GetTempPath() + _attachment.FileName;
            try
            {
                File.WriteAllBytes(filename, _attachment.File);
            }
            catch
            {
                MessageBox.Show("Could not show file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Task.Run(() => Process.Start(filename));
        }

        public void SaveFile()
        {
            var filename = _attachment.FileName;
            if (!GetImageFromDialog(ref filename)) return;

            try
            {
                File.WriteAllBytes(filename, _attachment.File);
            }
            catch
            {
                MessageBox.Show("Could not save file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool GetImageFromDialog(ref string filename)
        {
            // Create OpenFileDialog 
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                DefaultExt = ".*",
                Filter = "Any files (*.*)|*.*",
                FileName = filename
            };

            // Display OpenFileDialog by calling ShowDialog method 
            var result = dlg.ShowDialog();

            if (result != true) return false;
            filename = dlg.FileName;
            return true;
        }

        public AttachmentViewModel() { }

        public AttachmentViewModel(Attachment attachment)
        {
            Attachment = attachment;
        }
    }
}