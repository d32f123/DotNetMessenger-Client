using System;

namespace DotNetMessenger.WPFClient.Router
{
    public class WindowSettingsAttribute : Attribute
    {
        public bool ShouldShowModal { get; set; }
        public string WindowName { get; set; }

        public WindowSettingsAttribute(string name, bool isModal)
        {
            ShouldShowModal = isModal;
            WindowName = name;
        }
    }
}