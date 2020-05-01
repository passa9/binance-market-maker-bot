using System;
using System.Windows.Data;
using System.Windows.Media;

namespace BinanceMarketMaker.WPF.Converters
{
    class PriceChangeConverters : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return new SolidColorBrush(Colors.Black);

            SolidColorBrush brush = new SolidColorBrush(Colors.Black);
            double doubleValue = 0.0;
            Double.TryParse(value.ToString(), out doubleValue);

            if (doubleValue < 0)
                brush = new SolidColorBrush(Colors.Red);
            else if (doubleValue > 0)
                brush = new SolidColorBrush(Colors.Green);

            return brush;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

