using System;
using System.Globalization;
using System.Windows.Data;

namespace CitrixAI.Demo.Converters
{
    /// <summary>
    /// Converter that determines window size category for responsive styling.
    /// Supports Small, Medium, and Large window size classifications.
    /// </summary>
    public class WindowSizeConverter : IValueConverter
    {
        // Singleton instance for XAML static resource usage
        public static readonly WindowSizeConverter Instance = new WindowSizeConverter();

        // Size breakpoints for responsive behavior
        private const double SmallWindowThreshold = 1000;
        private const double LargeWindowThreshold = 1400;

        /// <summary>
        /// Converts window width to size category string.
        /// </summary>
        /// <param name="value">Window width as double</param>
        /// <param name="targetType">Target type (string)</param>
        /// <param name="parameter">Optional parameter</param>
        /// <param name="culture">Culture info</param>
        /// <returns>Size category: "Small", "Medium", or "Large"</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                if (width < SmallWindowThreshold)
                    return "Small";
                else if (width > LargeWindowThreshold)
                    return "Large";
                else
                    return "Medium";
            }

            return "Medium"; // Default fallback
        }

        /// <summary>
        /// Not implemented for one-way conversion.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("WindowSizeConverter is a one-way converter.");
        }
    }
}