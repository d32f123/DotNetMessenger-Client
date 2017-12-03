using DotNetMessenger.WPFClient.ViewModels;

namespace DotNetMessenger.WPFClient.Router
{
    public interface IViewHost
    {
        void HostView(ViewModelBase viewModel);
        void HostViewRegular(ViewModelBase viewModel);
        void HostViewModal(ViewModelBase viewModel);
    }
}