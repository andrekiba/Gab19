using System;
using System.Globalization;
using Xamarin.Forms;

namespace Gab.Converters
{
    public class BookedToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var booked = (bool) value;
            if (booked)
                return (Color)Application.Current.Resources["LightRed"];
                
                return Color.Default;
            }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
