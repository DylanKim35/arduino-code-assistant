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
        public IServiceProvider? ServiceProvider { get; private set; }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<ArduinoService>();
            services.AddSingleton<ArduinoInfo>();
            services.AddSingleton<SerialConfig>();
            services.AddSingleton<ChatService>();
            services.AddSingleton<ChatRequest>();
            services.AddSingleton<ChatResponse>();
            services.AddSingleton<AudioRecorder>();
            services.AddSingleton<WhisperService>();
            services.AddSingleton(provider =>
            {
                var mainWindow = provider.GetRequiredService<MainWindow>();
                return new LoggingService(mainWindow.LogRichTextBox, mainWindow.SerialRichTextBox, mainWindow.Dispatcher);
            });
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            var mainWindowViewModel = ServiceProvider.GetRequiredService<MainWindowViewModel>();

            Current.MainWindow = mainWindow;
            Current.MainWindow.DataContext = mainWindowViewModel;

            Current.MainWindow.Show();
        }
    }

}
