using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using Gab.Shared.Base;
using Plugin.Multilingual;
using Refit;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Gab.Base
{
    public static class StringExtensions
    {
        public static string Translate(this string error) => TranslationHelper.Translate(error);
    }

    public static class ColorExtensions
    {
        public static Color Lerp(this Color from, Color to, float amount)
        {
            double sr = from.R, sg = from.G, sb = from.B;

            double er = to.R, eg = to.G, eb = to.B;

            var r = sr.Lerp(er, amount);
            var g = sg.Lerp(eg, amount);
            var b = sb.Lerp(eb, amount);

            return Color.FromRgb(r, g, b);
        }

        public static List<Color> GenerateShades(this Color color, int howMany) => Enumerable.Range(1, howMany)
            .Reverse()
            .Select(n => n * 1 / (float)howMany)
            .Select(n => color.Lerp(Color.White, n))
            .ToList();

        public static double Lerp(this double start, double end, double by)
        {
            var normalized = Math.Max(Math.Min(by, 1), 0);
            return start * normalized + end * (1 - normalized);
        }

        public static string ToHex(this Color color)
        {
            var red = (int)(color.R * 255);
            var green = (int)(color.G * 255);
            var blue = (int)(color.B * 255);
            var alpha = (int)(color.A * 255);
            return $"#{alpha:X2}{red:X2}{green:X2}{blue:X2}";
        }

        public static Color Lighter(this Color color)
        {
            return color.WithLuminosity(color.Luminosity + 0.2);
        }

        public static Color Darker(this Color color)
        {
            return color.WithLuminosity(color.Luminosity - 0.2);
        }

        public static Color Lighten(this Color color, double amount)
        {
            return color.WithLuminosity(color.Luminosity + amount);
        }

        public static Color Darken(this Color color, double amaount)
        {
            return color.WithLuminosity(color.Luminosity - amaount);
        }

        public static Color Complement(this Color color)
        {
            var hue = (color.Hue * 359.0);
            var newHue = ((hue + 180) % 359.0);
            var complement = color.WithHue(newHue / 359.0);

            return complement;
        }

        public static Color Invert(this Color color)
        {
            var r = 255 - (int)(255 * color.R);
            var g = 255 - (int)(255 * color.G);
            var b = 255 - (int)(255 * color.B);
            return Color.FromRgb(r, g, b);
        }
    }

    [ContentProperty("Source")]
    public class ImageResourceExtension : IMarkupExtension
    {
        public string Source { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Source == null)
            {
                return null;
            }
            // Do your translation lookup here, using whatever method you require
            var imageSource = ImageSource.FromResource(Source);

            return imageSource;
        }
    }

    public static class PropertyChangedExtensions
    {
        public static void WhenPropertyChanged<T>(this T obj, string property,
            Action<T> action) where T : INotifyPropertyChanged
        {
            obj.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == property)
                    action((T)sender);
            };
        }

        public static void WhenPropertyChanged<T>(this T obj, string property,
            Predicate<T> predicate, Action<T> action) where T : INotifyPropertyChanged
        {
            obj.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == property && predicate((T)sender))
                    action((T)sender);
            };
        }

        public static IObservable<string> WhenPropertyChanged<T>(this T obj)
            where T : INotifyPropertyChanged
        {
            var obs = Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    h => obj.PropertyChanged += h,
                    h => obj.PropertyChanged -= h)
                .Select(x => x.EventArgs.PropertyName);

            return obs;
        }

        public static void WhenCollectionChanged<T>(this T obj, Action<T> action)
            where T : INotifyCollectionChanged
        {
            obj.CollectionChanged += (sender, e) =>
            {
                action((T)sender);
            };
        }

        public static void WhenCollectionChanged<T>(this T obj,
            Func<T, NotifyCollectionChangedEventArgs, bool> predicate, Action<T> action)
            where T : INotifyCollectionChanged
        {
            obj.CollectionChanged += (sender, e) =>
            {
                if (predicate((T)sender, e))
                    action((T)sender);
            };
        }       

        public static IObservable<NotifyCollectionChangedEventArgs> WhenCollectionChanged<T>(this T obj)
            where T : INotifyCollectionChanged
        {
            var obs = Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => obj.CollectionChanged += h,
                h => obj.CollectionChanged -= h)
                .Select(x => x.EventArgs);

            return obs;
        }
    }

    public static class RefitApiExceptionExtensions
    {
        public static async Task<string> GetMessage(this ApiException apiEx)
        {
            var message = apiEx.Message;

            if (!apiEx.HasContent)
                return message;
            try
            {
                var result = await apiEx.GetContentAsAsync<Result>();
                if(result.IsFailure)
                    message = result.Error;
            }
            catch (Exception)
            {
                // ignored
            }

            return message;
        }
    }

    public static class ObservableExtensions
    {
        public static IDisposable SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNext, Action<Exception> onError, Action onCompleted)
        {
            return source.Select(e => Observable.Defer(() => onNext(e).ToObservable())).Concat()
                .Subscribe(
                    e => { }, // empty
                    onError,
                    onCompleted
                );
        }
    }

    public static class DictioanaryExtensions
    {
        public static IDictionary<T1, T2> Merge<T1, T2>(this IDictionary<T1, T2> dictionary, IDictionary<T1, T2> newElements)
        {
            if (newElements == null || newElements.Count == 0)
                return dictionary;

            foreach (var e in newElements)
            {
                dictionary.Remove(e.Key);
                dictionary.Add(e);
            }

            return dictionary;
        }
    }

    [ContentProperty("Text")]
    public class TranslateExtension : IMarkupExtension
    {
        const string ResourceId = "Gab.Resources.AppResources";

        static readonly Lazy<ResourceManager> resmgr = new Lazy<ResourceManager>(() => new ResourceManager(ResourceId, typeof(TranslateExtension).GetTypeInfo().Assembly));

        public string Text { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Text == null)
                return string.Empty;

            var ci = CrossMultilingual.Current.CurrentCultureInfo;

            var translation = resmgr.Value.GetString(Text, ci);

            if (translation == null)
            {
#if DEBUG
                throw new ArgumentException($@"Key '{Text}' was not found in resources '{ResourceId}' for culture '{ci.Name}'.", nameof(Text));
#else
				translation = Text; // returns the key, which GETS DISPLAYED TO THE USER
#endif
            }
            return translation;
        }
    }
}
