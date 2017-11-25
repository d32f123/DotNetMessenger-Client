using DotNetMessenger.WPFClient.ViewModels;

namespace DotNetMessenger.WPFClient.Router
{
    public interface IViewHost
    {
        void HostView(ViewModelBase viewModel);
        void HostViewModal(ViewModelBase viewModel);
    }
}