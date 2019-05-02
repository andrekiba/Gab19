using System;
using System.Globalization;
using System.Linq;
using Xamarin.Forms;

namespace Gab.Converters
{
    public class NameToInitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
            var words = value.ToString().Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).Take(2);
            var result = words.Aggregate("", (current, word) => current + word[0]);
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
