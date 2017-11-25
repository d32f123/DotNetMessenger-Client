namespace DotNetMessenger.WPFClient.Extensions
{
    public class FileDialogHelpers
    {
        public static bool GetImageFromDialog(ref string filename)
        {
            // Create OpenFileDialog 
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".*",
                Filter = "Any files (*.*)|*.*"
            };

            // Display OpenFileDialog by calling ShowDialog method 
            var result = dlg.ShowDialog();

            if (result != true) return false;
            filename = dlg.FileName;
            return true;
        }
    }
}