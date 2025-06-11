using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CitrixAI.Demo.Views
{
    /// <summary>
    /// Interaction logic for AnnotationWindow.xaml
    /// </summary>
    public partial class AnnotationWindow : Window
    {
        private bool _isDrawing;
        private Point _startPoint;
        private Rectangle _currentRectangle;

        public AnnotationWindow(BitmapImage image)
        {
            InitializeComponent();
            AnnotationImage.Source = image;

            AnnotationImage.MouseMove += AnnotationImage_MouseMove;
            AnnotationImage.MouseLeftButtonUp += AnnotationImage_MouseLeftButtonUp;
        }

        private void AnnotationImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(AnnotationCanvas);
            _isDrawing = true;
            AnnotationImage.CaptureMouse();
        }

        private void AnnotationImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing)
            {
                var currentPoint = e.GetPosition(AnnotationCanvas);

                if (_currentRectangle == null)
                {
                    _currentRectangle = new Rectangle
                    {
                        Stroke = Brushes.Red,
                        StrokeThickness = 2,
                        Fill = Brushes.Transparent
                    };
                    AnnotationCanvas.Children.Add(_currentRectangle);
                }

                var left = Math.Min(_startPoint.X, currentPoint.X);
                var top = Math.Min(_startPoint.Y, currentPoint.Y);
                var width = Math.Abs(currentPoint.X - _startPoint.X);
                var height = Math.Abs(currentPoint.Y - _startPoint.Y);

                Canvas.SetLeft(_currentRectangle, left);
                Canvas.SetTop(_currentRectangle, top);
                _currentRectangle.Width = width;
                _currentRectangle.Height = height;
            }
        }

        private void AnnotationImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawing)
            {
                _isDrawing = false;
                _currentRectangle = null;
                AnnotationImage.ReleaseMouseCapture();
                StatusText.Text = $"Annotation added. Total annotations: {AnnotationCanvas.Children.Count - 1}";
            }
        }

        private void SaveAnnotations_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Annotation saving will be implemented in future iterations", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadAnnotations_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Annotation loading will be implemented in future iterations", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddRectangle_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Click and drag on the image to create a rectangle annotation";
        }

        private void ClearAnnotations_Click(object sender, RoutedEventArgs e)
        {
            // Remove all rectangles, keeping only the image
            for (int i = AnnotationCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (AnnotationCanvas.Children[i] is Rectangle)
                {
                    AnnotationCanvas.Children.RemoveAt(i);
                }
            }
            StatusText.Text = "All annotations cleared";
        }

        
    }
}