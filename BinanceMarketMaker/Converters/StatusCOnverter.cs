
using BinanceMarketMaker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace BinanceMarketMaker.WPF.Converters
{
    class StatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            SolidColorBrush brush;
            Status status = (Status)value;

            if (status == Status.Buy)
            {
                brush = new SolidColorBrush(Colors.Green);
            }
            else if (status == Status.Sell)
            {
                brush = new SolidColorBrush(Colors.Red);
            }
            else if (status == Status.WaitBuy || status == Status.WaitSell)
            {
                brush = new SolidColorBrush(Colors.Orange);
            }
            else if (status == Status.Completed)
            {
                brush = new SolidColorBrush(Colors.Green);
            }
            else if (status == Status.Error)
            {
                brush = new SolidColorBrush(Colors.Red);
            }
            else
            {
                throw new NotImplementedException();
            }

            return brush;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

