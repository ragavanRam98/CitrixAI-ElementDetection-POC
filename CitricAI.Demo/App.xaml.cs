using System.Windows;

namespace CitrixAI.Demo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize logging or other startup tasks here
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Cleanup resources
            base.OnExit(e);
        }
    }
}