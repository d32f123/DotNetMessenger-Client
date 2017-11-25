using DotNetMessenger.WPFClient.ViewModels;

namespace DotNetMessenger.WPFClient.Router
{
    public class WpfViewHost : IViewHost
    {
        public void HostView(ViewModelBase viewModel)
        {
            var window = new WPFViewModelPresenter {DataContext = viewModel};
            viewModel.CloseRequested += (sender, args) => window.Close();
            window.Show();
        }

        public void HostViewModal(ViewModelBase viewModel)
        {
            var window = new WPFViewModelPresenter {DataContext = viewModel};
            viewModel.CloseRequested += (sender, args) => window.Close();
            window.ShowDialog();
        }
    }
}