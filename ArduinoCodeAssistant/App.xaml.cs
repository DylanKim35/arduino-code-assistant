using ArduinoCodeAssistant.Models;
using ArduinoCodeAssistant.Services;
using ArduinoCodeAssistant.ViewModels;
using ArduinoCodeAssistant.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace ArduinoCodeAssistant
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<ArduinoService>();
            services.AddSingleton<ArduinoInfo>();
            services.AddSingleton<ChatService>();
            services.AddSingleton<ChatRequest>();
            services.AddSingleton<ChatResponse>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            Current.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
            Current.MainWindow.Show();
        }
    }

}
