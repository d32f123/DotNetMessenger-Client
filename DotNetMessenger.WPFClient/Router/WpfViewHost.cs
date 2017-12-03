using System;
using DotNetMessenger.WPFClient.ViewModels;

namespace DotNetMessenger.WPFClient.Router
{
    public class WpfViewHost : IViewHost
    {
        public void HostView(ViewModelBase viewModel)
        {
            var info =
                (WindowSettingsAttribute)Attribute.GetCustomAttribute(viewModel.GetType(), typeof(WindowSettingsAttribute));

            var doModal = info?.ShouldShowModal;

            if (doModal != true)
            {
                HostViewRegular(viewModel);
            }
            else
            {
                HostViewModal(viewModel);
            }
        }

        public void HostViewRegular(ViewModelBase viewModel)
        {
            var info =
                (WindowSettingsAttribute)Attribute.GetCustomAttribute(viewModel.GetType(), typeof(WindowSettingsAttribute));

            var window = new WPFViewModelPresenter {DataContext = viewModel};

            if (!string.IsNullOrEmpty(info?.WindowName))
                window.Title = info.WindowName;

            viewModel.CloseRequested += (sender, args) => window.Close();
            window.Show();
        }

        public void HostViewModal(ViewModelBase viewModel)
        {
            var info =
                (WindowSettingsAttribute)Attribute.GetCustomAttribute(viewModel.GetType(), typeof(WindowSettingsAttribute));

            var window = new WPFViewModelPresenter {DataContext = viewModel};

            if (!string.IsNullOrEmpty(info?.WindowName))
                window.Title = info.WindowName;

            viewModel.CloseRequested += (sender, args) => window.Close();
            window.ShowDialog();
        }
    }
}