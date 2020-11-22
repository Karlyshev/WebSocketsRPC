using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WebSocketManager.Ext;
using WebSocketManager.Services;
using WebSocketManager.ViewModels;

namespace WebSocketManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;
        public static ServiceProvider ServiceProvider { get; private set; }

        private void ConfigureServices(IServiceCollection services) 
        {
            //services.AddSingleton<ILogBase>(new LogBase(new FileInfo($@"C:\temp\log.txt")));
            services.AddSingleton<SessionCollection>();
            services.AddSingleton<HubService>();
            services.AddSingleton<MainVM>();
            services.AddSingleton<MainWindow>();
        }

        private void AppOnStartup(object sender, StartupEventArgs e) 
        {
            var mainWindow = _serviceProvider.GetService<MainWindow>();
            mainWindow.Show();
        }

        public App() 
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();
            ServiceProvider = _serviceProvider;
        }
    }
}
