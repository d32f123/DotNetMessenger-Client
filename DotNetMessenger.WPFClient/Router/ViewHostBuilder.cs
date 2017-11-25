namespace DotNetMessenger.WPFClient.Router
{
    public static class ViewHostBuilder
    {
        public static IViewHost GetViewHost()
        {
            return new WpfViewHost();
        }
    }
}