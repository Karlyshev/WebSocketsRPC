using System.Windows;
using WebSocketManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace WebSocketManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider.GetService<MainVM>();
        }
    }
}
