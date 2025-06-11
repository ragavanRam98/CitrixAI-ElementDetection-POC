using CitrixAI.Demo.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace CitrixAI.Demo.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SourceImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                var position = e.GetPosition(SourceImage);
                viewModel.HandleImageClick(position.X, position.Y);
            }
        }
    }
}