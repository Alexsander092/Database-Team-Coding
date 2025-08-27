using System.Globalization;
using Microsoft.Maui.Controls;

namespace Oracle_Version_Control.Converters
{
    public class StringEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;
                
            bool isEqual = value.ToString() == parameter.ToString();
            
            if (targetType == typeof(Color) || targetType == typeof(Microsoft.Maui.Graphics.Color))
            {
                if (isEqual)
                    return Color.FromArgb("#E3F2FD"); 
                else
                    return Color.FromArgb("#FFFFFF"); 
            }
            
            if (targetType == typeof(Brush))
            {
                if (isEqual)
                    return new SolidColorBrush(Color.FromArgb("#0072C6")); 
                else
                    return new SolidColorBrush(Colors.Transparent); 
            }

            return isEqual;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class NotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class NotNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;
                
            return !string.IsNullOrEmpty(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class AlternatingRowColorConverter : IValueConverter
    {
        private static readonly Color EvenRowColor = Color.FromArgb("#212121");
        private static readonly Color OddRowColor = Color.FromArgb("#404040");
        private static readonly Color EvenRowTextColor = Colors.White;
        private static readonly Color OddRowTextColor = Colors.White;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return EvenRowColor;

            if (parameter is string textParam && textParam == "TextColor")
            {
                return Colors.White;
            }

            return EvenRowColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class IndexToColorConverter : IValueConverter
    {
        private static readonly Color EvenRowColor = Color.FromArgb("#212121");
        private static readonly Color OddRowColor = Color.FromArgb("#404040");
        private static readonly Color TextColor = Colors.White;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not int index)
                return EvenRowColor;

            if (parameter is string textParam && textParam == "TextColor")
            {
                return TextColor;
            }

            return index % 2 == 0 ? EvenRowColor : OddRowColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class DirectionSymbolConverter : IValueConverter
    {
        private static readonly string BLACK_UP_TRIANGLE = "\u25B2";
        private static readonly string BLACK_DOWN_TRIANGLE = "\u25BC";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAscending)
            {
                return isAscending ? BLACK_UP_TRIANGLE : BLACK_DOWN_TRIANGLE;
            }
            
            return BLACK_DOWN_TRIANGLE;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}