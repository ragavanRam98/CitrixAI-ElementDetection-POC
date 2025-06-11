using System.Windows;

namespace CitrixAI.Demo.Views
{
    /// <summary>
    /// Interaction logic for MockCitrixWindow.xaml
    /// </summary>
    public partial class MockCitrixWindow : Window
    {
        public MockCitrixWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Mock login - this is for testing UI element detection", "Mock Action",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}